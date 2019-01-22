using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using LanguageExt;
using Newtonsoft.Json;
using Telega.Internal;
using Telega.Rpc.Dto.Types;
using static LanguageExt.Prelude;

namespace Telega.Example
{
    static class Program
    {
        static async Task SignInViaCode(TelegramClient tg, Config cfg)
        {
            var codeHash = await tg.Auth.SendCode(cfg.Phone);

            while (true)
            {
                try
                {
                    Console.WriteLine("Enter the telegram code");
                    var code = Console.ReadLine();
                    await tg.Auth.SignIn(cfg.Phone, codeHash, code);
                }
                catch (TgInvalidPhoneCodeException)
                {
                    Console.WriteLine("Invalid phone code");
                }
            }
        }

        static async Task SignInViaPassword(TelegramClient tg, Config cfg)
        {
            var pwdInfo = await tg.Auth.GetPasswordInfo();
            await tg.Auth.CheckPassword(pwdInfo, cfg.Password);
        }

        static async Task EnsureAuthorized(TelegramClient tg, Config cfg)
        {
            if (tg.IsAuthorized)
            {
                Console.WriteLine("Already authorized");
                return;
            }

            try
            {
                await SignInViaCode(tg, cfg);
            }
            catch (TgPasswordNeededException)
            {
                await SignInViaPassword(tg, cfg);
            }

            Console.WriteLine("Authorization completed");
        }

        static Task<Config> ReadConfig() =>
            File.ReadAllTextAsync("config.json").Map(JsonConvert.DeserializeObject<Config>);

        static async Task DownloadFirstChannelPictureExample(TelegramClient tg)
        {
            var chatsType = await tg.Messages.GetDialogs();
            var chats = chatsType.AsTag().IfNone(() => throw new NotImplementedException());
            var channels = chats.Chats.Choose(Chat.AsChannelTag);

            var sns = channels
                .HeadOrNone()
                .IfNone(() => throw new Exception("A channel is not found"));
            var photo = sns.Photo
                .AsTag().IfNone(() => throw new Exception("The first channel does not have a photo"));
            var bigPhotoFile = photo.PhotoBig
                .AsTag().IfNone(() => throw new Exception("The first channel photo is unavailable"));

            InputFileLocation ToInput(FileLocation.Tag location) =>
                new InputFileLocation.Tag(
                    volumeId: location.VolumeId,
                    localId: location.LocalId,
                    secret: location.Secret,
                    fileReference: location.FileReference
                );

            var photoLoc = ToInput(bigPhotoFile);
            var fileType = await tg.GetFileType(photoLoc);
            var fileTypeExt = fileType.Match(
                pngTag: _ => ".png",
                jpegTag: _ => ".jpeg",
                _: () => throw new NotImplementedException()
            );

            using (var fs = File.OpenWrite($"channel-photo{fileTypeExt}"))
            {
                await tg.DownloadFile(fs, photoLoc);
            }
        }

        static async Task SendOnePavelDurovPictureToMeExample(TelegramClient tg)
        {
            const string photoUrl = "https://cdn1.img.jp.sputniknews.com/images/406/99/4069980.png";
            var photoName = Path.GetFileName(photoUrl);
            var photo = new WebClient().DownloadData(photoUrl);

            var tgPhoto = await tg.UploadFile(photoName, photo.Length, new MemoryStream(photo));
            await tg.Messages.SendPhoto(
                peer: new InputPeer.SelfTag(),
                file: tgPhoto,
                message: "Sent from Telega"
            );
        }

        static async Task Main()
        {
            // it is disabled by default
            Internal.TgTrace.IsEnabled = false;

            var cfg = await ReadConfig();
            using (var tg = await TelegramClient.Connect(cfg.ApiId, cfg.ApiHash))
            {
                await EnsureAuthorized(tg, cfg);

                await SendOnePavelDurovPictureToMeExample(tg);
            }
        }
    }
}
