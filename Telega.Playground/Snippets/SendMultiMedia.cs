using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Telega.Client;
using Telega.Rpc.Dto.Types;

namespace Telega.Playground.Snippets {
    static class SendMultiMedia {
        static async Task<MessageMedia> UploadPhoto(
            TelegramClient tg,
            string photoName,
            byte[] bytes,
            InputPeer peer
        ) {
            var photo = await tg.Upload.UploadFile(
                name: photoName,
                fileLength: bytes.Length,
                stream: new MemoryStream(buffer: bytes)
            );
            return await tg.Messages.UploadMediaAsPhoto(
                peer: peer,
                file: photo
            );
        }

        static async Task<MessageMedia> UploadVideo(
            TelegramClient tg,
            string videoName,
            byte[] bytes,
            InputPeer peer,
            string mimeType
        ) {
            var video = await tg.Upload.UploadFile(
                name: videoName,
                fileLength: bytes.Length,
                stream: new MemoryStream(buffer: bytes)
            );
            return await tg.Messages.UploadMediaAsDocument(
                peer: peer,
                file: video,
                mimeType: mimeType
            );
        }

        public static async Task Run(TelegramClient tg) {
            Console.WriteLine("Downloading multimedia files from web.");
            const string photoUrl = "https://cdn1.img.jp.sputniknews.com/images/406/99/4069980.png";
            var photoName = Path.GetFileName(path: photoUrl);
            var photo = new WebClient().DownloadData(address: photoUrl);

            const string videoUrl = "http://techslides.com/demos/sample-videos/small.mp4";
            var videoName = Path.GetFileName(path: videoUrl);
            var video = new WebClient().DownloadData(address: videoUrl);

            var inputPeer = new InputPeer.SelfTag();

            Console.WriteLine("Uploading multimedia.");
            var sentImage = await UploadPhoto(tg, photoName, photo, inputPeer);
            var sentVideo = await UploadVideo(tg, videoName, video, inputPeer, "video/mp4");
            await tg.Messages.SendMultimedia(
                peer: inputPeer,
                message: "Sent from Telega",
                attachments: new[] {
                    sentImage,
                    sentVideo
                }
            );
            
            Console.WriteLine("Multimedia is sent to saved messages.");
        }
    }
}