using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NullExtensions;
using Telega.Client;
using Telega.Rpc.Dto.Functions.Messages;
using Telega.Rpc.Dto.Functions.Users;
using Telega.Rpc.Dto.Types;

namespace Telega.Playground.Snippets {
    static class DownloadLastMovieFromSavedMessages {
        static async Task<IReadOnlyList<Document.DefaultTag>> ScrapeHistoryDocuments(
            TelegramClient tg,
            InputPeer peer,
            int offset = 0
        ) {
            const int batchLimit = 100;
            var resp = await tg.Call(new GetHistory(
                peer: peer,
                addOffset: offset,
                limit: batchLimit,
                minId: 0,
                maxId: 0,
                hash: 0,
                offsetDate: 0,
                offsetId: 0
            ));
            var messages = resp.Slice_!.Messages;
            var documents = messages
                .Reverse()
                .NChoose(x => x.Default)
                .NChoose(message => message.Media)
                .NChoose(x => x.Document)
                .NChoose(x => x.Document)
                .NChoose(x => x.Default);
            return messages.Count == 0
                ? documents.ToList()
                : (await ScrapeHistoryDocuments(tg, peer, offset + batchLimit)).Concat(documents).ToList();
        }

        public static async Task Run(TelegramClient tg) {
            var fullUserInfo = await tg.Call(new GetFullUser(new InputUser.Self_Tag()));
            var userInfo = fullUserInfo.Users.Single().Default!;

            var chatPeer = (InputPeer) new InputPeer.User_Tag(
                userId: userInfo.Id,
                accessHash: userInfo.AccessHash!.Value
            );

            var history = await ScrapeHistoryDocuments(tg, chatPeer);
            var video = history.Last(x => x.Attributes.NChoose(x => x.Video).Any());
            var videoName = video.Attributes.NChoose(x => x.Filename).Single().FileName;

            Console.WriteLine($"Downloading the video with name '{videoName}'.");
            var videoLocation = new InputFileLocation.Encrypted_Tag(
                id: video.Id,
                accessHash: video.AccessHash
            );
            await using var fs = File.OpenWrite(videoName);
            await tg.Upload.DownloadFile(fs, videoLocation);
            Console.WriteLine("The video is downloaded.");
        }
    }
}