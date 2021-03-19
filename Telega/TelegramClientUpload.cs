using LanguageExt;
using Telega.Connect;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Telega.Rpc.Dto;
using Telega.Rpc.Dto.Functions.Upload;
using Telega.Rpc.Dto.Types;
using Telega.Rpc.Dto.Types.Storage;
using Telega.Utils;
using File = Telega.Rpc.Dto.Types.Upload.File;
using static LanguageExt.Prelude;

namespace Telega
{
    public sealed class TelegramClientUpload
    {
        readonly TgBellhop _tg;
        internal TelegramClientUpload(Some<TgBellhop> tg) => _tg = tg;

        const int ChunkSize = 512 * 1024;

        static bool IsBigUpload(int size) => size > 10 * 1024 * 1024;

        static async Task ReadToBuffer(byte[] buffer, int pos, int count, Stream stream)
        {
            var totalReceived = 0;
            while (totalReceived < count)
            {
                var received = await stream.ReadAsync(buffer, pos + totalReceived, count - totalReceived).ConfigureAwait(false);
                if (received == 0) throw new EndOfStreamException();
                totalReceived += received;
            }
        }

        public async Task<InputFile> UploadFile(
            Some<string> name,
            long fileId,
            int fileLength,
            Some<Stream> stream
        ) {
            if (fileLength <= 0) throw new ArgumentOutOfRangeException(nameof(fileLength));

            var tg = _tg.Fork();

            var isBigFileUpload = IsBigUpload(fileLength);
            var buffer = new byte[ChunkSize];
            var md5 = isBigFileUpload ? null : MD5.Create();

            var totalReceived = 0;
            var chunkIdx = 0;
            var chunksCount = 1 + (fileLength - 1) / ChunkSize;

            while (chunkIdx < chunksCount)
            {
                var chunkSize = Math.Min(ChunkSize, fileLength - totalReceived);
                await ReadToBuffer(buffer, 0, chunkSize, stream).ConfigureAwait(false);
                totalReceived += chunkSize;
                md5?.TransformBlock(buffer, 0, chunkSize, buffer, 0);

                var res = await tg.Call(isBigFileUpload
                    ? (ITgFunc<bool>) new SaveBigFilePart(
                        fileId: fileId,
                        filePart: chunkIdx++,
                        bytes: buffer.ToBytesUnsafe(),
                        fileTotalParts: chunksCount
                    )
                    : new SaveFilePart(
                        fileId: fileId,
                        filePart: chunkIdx++,
                        bytes: buffer.ToBytesUnsafe()
                    )
                ).ConfigureAwait(false);
                Helpers.Assert(res, "chunk send failed");
            }

            var md5Hash = md5?.TransformFinalBlock(buffer, 0, 0);

            return isBigFileUpload
                ? (InputFile) new InputFile.BigTag(
                    id: fileId,
                    name: name,
                    parts: chunksCount
                )
                : (InputFile) new InputFile.Tag(
                    id: fileId,
                    name: name,
                    parts: chunksCount,
                    md5Checksum: md5Hash.Apply(BitConverter.ToString).Replace("-", "").ToLower()
                );
        }

        public Task<InputFile> UploadFile(
            Some<string> name,
            int fileLength,
            Some<Stream> stream
        ) => UploadFile(name, Rnd.NextInt64(), fileLength, stream);

        public async Task<InputFile> UploadFile(
            Some<string> filePath
        ) {
            var name = Path.GetFileName(filePath);
            using var fs = System.IO.File.OpenRead(filePath);
            if (fs.Length > int.MaxValue) throw new ArgumentException("the file is too big", nameof(filePath));
            return await UploadFile(name, (int) fs.Length, fs).ConfigureAwait(false);
        }


        static GetFile GenSmallestGetFileRequest(Some<InputFileLocation> location) => new(
            precise: true,
            cdnSupported: false,
            location: location,
            offset: 0,
            limit: 4 * 1024
        );

        public async Task<FileType> GetFileType(
            Some<InputFileLocation> location
        ) {
            var tg = _tg.Fork();
            var resp = await tg.Call(GenSmallestGetFileRequest(location)).ConfigureAwait(false);
            var res = resp.Match(
                tag: identity,
                cdnRedirectTag: _ => throw Helpers.FailedAssertion("upload.fileCdnRedirect")
            );
            return res.Type;
        }

        public async Task<FileType> DownloadFile(
            Some<Stream> someStream,
            Some<InputFileLocation> location
        ) {
            var tg = _tg.Fork();

            var stream = someStream.Value;
            var offset = 0;
            var prevFile = default(File.Tag);
            while (true)
            {
                var resp = await tg.Call(new GetFile(
                    precise: true,
                    cdnSupported: false,
                    location: location,
                    offset: offset,
                    limit: ChunkSize
                )).ConfigureAwait(false);
                var res = prevFile = resp.Match(
                    tag: identity,
                    cdnRedirectTag: _ => throw Helpers.FailedAssertion("upload.fileCdnRedirect")
                );

                var bts = res.Bytes.ToArrayUnsafe();
                await stream.WriteAsync(bts, 0, bts.Length).ConfigureAwait(false);
                offset += bts.Length;

                if (bts.Length < ChunkSize) break;
            }

            return prevFile.Type;
        }
    }
}
