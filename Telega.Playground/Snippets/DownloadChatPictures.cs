using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NullExtensions;
using Telega.Client;
using Telega.Rpc.Dto.Functions.Messages;
using Telega.Rpc.Dto.Types;

namespace Telega.Playground.Snippets {
    static class DownloadChatPictures {
        static async Task<IReadOnlyList<Photo.DefaultTag>> ScrapeHistoryPhotos(
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
            var messages = resp.Slice!.Messages;
            var photos = messages
                .Reverse()
                .NChoose(x => x.Default)
                .NChoose(message => message.Media)
                .NChoose(x => x.Photo)
                .NChoose(x => x.Photo)
                .NChoose(x => x.Default);
            return messages.Count == 0
                ? photos.ToList()
                : (await ScrapeHistoryPhotos(tg, peer, offset + batchLimit)).Concat(photos).ToList();
        }

        public static async Task Run(TelegramClient tg) {
            const string chatName = "Amsterdam";
            const string counterFormat = "000";

            var dialogs = await tg.Messages.GetDialogs();
            var chat = dialogs.Default!.Chats.NChoose(x => x.Default).Single(x => x.Title == chatName);
            var chatPeer = new InputPeer.ChatTag(chatId: chat.Id);

            Console.WriteLine("Scraping chat messages.");
            var allPhotos = await ScrapeHistoryPhotos(tg, chatPeer);

            const string photosDir = chatName;
            if (!Directory.Exists(photosDir)) {
                Directory.CreateDirectory(photosDir);
            }

            Console.WriteLine("Downloading images.");
            var counter = 1;
            foreach (var photo in allPhotos) {
                var biggestSize = photo.Sizes.NChoose(x => x.Default).OrderByDescending(x => x.Size).First();
                var photoFileLocation = new InputFileLocation.PhotoTag(
                    id: photo.Id,
                    accessHash: photo.AccessHash,
                    fileReference: photo.FileReference,
                    thumbSize: biggestSize.Type
                );

                var fileType = await tg.Upload.GetFileType(photoFileLocation);
                var fileTypeExt = fileType.Match(
                    pngTag: _ => ".png",
                    jpegTag: _ => ".jpg",
                    _: () => throw new NotImplementedException()
                );

                var counterStr = counter++.ToString(counterFormat);
                var photoName = $"{counterStr}{fileTypeExt}";
                var photoPath = Path.Combine(photosDir, photoName);

                await using var fileStream = File.OpenWrite(photoPath);
                await tg.Upload.DownloadFile(fileStream, photoFileLocation);

                Console.WriteLine($"{counterStr}/{allPhotos.Count} downloaded.");
            }
        }
    }
}