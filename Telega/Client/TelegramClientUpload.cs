using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Telega.Connect;
using Telega.Rpc.Dto;
using Telega.Rpc.Dto.Functions.Upload;
using Telega.Rpc.Dto.Types;
using Telega.Rpc.Dto.Types.Storage;
using Telega.Utils;
using File = Telega.Rpc.Dto.Types.Upload.File;

namespace Telega.Client {
    public sealed class TelegramClientUpload {
        readonly TgBellhop _tg;
        internal TelegramClientUpload(TgBellhop tg) => _tg = tg;

        const int ChunkSize = 512 * 1024;

        static bool IsBigUpload(int size) => size > 10 * 1024 * 1024;

        static async Task ReadToBuffer(byte[] buffer, int pos, int count, Stream stream) {
            var totalReceived = 0;
            while (totalReceived < count) {
                var received = await stream.ReadAsync(buffer, pos + totalReceived, count - totalReceived).ConfigureAwait(false);
                if (received == 0) {
                    throw new EndOfStreamException();
                }

                totalReceived += received;
            }
        }

        public async Task<InputFile> UploadFile(
            string name,
            long fileId,
            int fileLength,
            Stream stream
        ) {
            if (fileLength <= 0) {
                throw new ArgumentOutOfRangeException(nameof(fileLength));
            }

            var tg = _tg.Fork();

            var isBigFileUpload = IsBigUpload(fileLength);
            var buffer = new byte[ChunkSize];
            var md5 = isBigFileUpload ? null : MD5.Create();

            var totalReceived = 0;
            var chunksCount = 1 + (fileLength - 1) / ChunkSize;

            for (var chunkIdx = 0; chunkIdx < chunksCount; chunkIdx++) {
                var chunkSize = Math.Min(ChunkSize, fileLength - totalReceived);
                await ReadToBuffer(buffer, 0, chunkSize, stream).ConfigureAwait(false);
                totalReceived += chunkSize;
                md5?.TransformBlock(buffer, 0, chunkSize, buffer, 0);

                var res = await tg.Call(isBigFileUpload
                    ? (ITgFunc<bool>) new SaveBigFilePart(
                        fileId: fileId,
                        filePart: chunkIdx,
                        bytes: buffer.Take(chunkSize).ToArray().ToBytesUnsafe(),
                        fileTotalParts: chunksCount
                    )
                    : new SaveFilePart(
                        fileId: fileId,
                        filePart: chunkIdx,
                        bytes: buffer.Take(chunkSize).ToArray().ToBytesUnsafe()
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
                : (InputFile) new InputFile.DefaultTag(
                    id: fileId,
                    name: name,
                    parts: chunksCount,
                    md5Checksum: md5Hash.Apply(BitConverter.ToString).Replace("-", "").ToLower()
                );
        }

        public Task<InputFile> UploadFile(
            string name,
            int fileLength,
            Stream stream
        ) => UploadFile(name, Rnd.NextInt64(), fileLength, stream);

        public async Task<InputFile> UploadFile(
            string filePath
        ) {
            var name = Path.GetFileName(filePath);
            using var fs = System.IO.File.OpenRead(filePath);
            if (fs.Length > int.MaxValue) {
                throw new ArgumentException("the file is too big", nameof(filePath));
            }

            return await UploadFile(name, (int) fs.Length, fs).ConfigureAwait(false);
        }


        static GetFile GenSmallestGetFileRequest(InputFileLocation location) => new(
            precise: true,
            cdnSupported: false,
            location: location,
            offset: 0,
            limit: 4 * 1024
        );

        public async Task<FileType> GetFileType(
            InputFileLocation location
        ) {
            var tg = _tg.Fork();
            var resp = await tg.Call(GenSmallestGetFileRequest(location)).ConfigureAwait(false);
            var res = resp.Match(
                defaultTag: x => x,
                cdnRedirectTag: _ => throw Helpers.FailedAssertion("upload.fileCdnRedirect")
            );
            return res.Type;
        }

        public async Task<FileType> DownloadFile(
            Stream stream,
            InputFileLocation location
        ) {
            var tg = _tg.Fork();

            var offset = 0;
            var prevFile = default(File.DefaultTag);
            while (true) {
                var resp = await tg.Call(new GetFile(
                    precise: true,
                    cdnSupported: false,
                    location: location,
                    offset: offset,
                    limit: ChunkSize
                )).ConfigureAwait(false);
                var res = prevFile = resp.Match(
                    defaultTag: x => x,
                    cdnRedirectTag: _ => throw Helpers.FailedAssertion("upload.fileCdnRedirect")
                );

                var bts = res.Bytes.ToArrayUnsafe();
                await stream.WriteAsync(bts, 0, bts.Length).ConfigureAwait(false);
                offset += bts.Length;

                if (bts.Length < ChunkSize) {
                    break;
                }
            }

            return prevFile.Type;
        }
    }
}
