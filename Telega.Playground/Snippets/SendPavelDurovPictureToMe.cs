using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Telega.Client;
using Telega.Rpc.Dto.Types;

namespace Telega.Playground.Snippets {
    static class SendPavelDurovPictureToMe {
        public static async Task Run(TelegramClient tg) {
            const string photoUrl = "https://cdn1.img.jp.sputniknews.com/images/406/99/4069980.png";
            var photoName = Path.GetFileName(photoUrl);
            var photo = new WebClient().DownloadData(photoUrl);

            var tgPhoto = await tg.Upload.UploadFile(photoName, photo.Length, new MemoryStream(photo));
            await tg.Messages.SendPhoto(
                peer: new InputPeer.SelfTag(),
                file: tgPhoto,
                message: "Sent from Telega",
                scheduleDate: null
            );
            Console.WriteLine("The proto is sent to saved messages.");
        }
    }
}