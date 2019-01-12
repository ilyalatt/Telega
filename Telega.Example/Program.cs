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
            var codeHash = await tg.SendCodeRequest(cfg.Phone);

            while (true)
            {
                try
                {
                    Console.WriteLine("Enter the telegram code");
                    var code = Console.ReadLine();
                    await tg.MakeAuth(cfg.Phone, codeHash, code);
                }
                catch (TgInvalidPhoneCodeException)
                {
                    Console.WriteLine("Invalid phone code");
                }
            }
        }

        static async Task SignInViaPassword(TelegramClient tg, Config cfg)
        {
            var pwdSettings = await tg.GetPasswordSetting();
            var pwd = pwdSettings.Match(
                tag: identity,
                noTag: _ => throw new Exception("WTF")
            );
            await tg.MakeAuthWithPassword(pwd, cfg.Password);
        }

        static async Task EnsureAuthenticated(TelegramClient tg, Config cfg)
        {
            if (tg.IsAuthenticated())
            {
                Console.WriteLine("Already authenticated");
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

            Console.WriteLine("Authentication completed");
        }

        static Task<Config> ReadConfig() =>
            File.ReadAllTextAsync("config.json").Map(JsonConvert.DeserializeObject<Config>);

        static async Task SnsExample(TelegramClient tg)
        {
            var chatsType = await tg.GetUserDialogs();
                var chats = chatsType.Match(
                    tag: identity,
                    _: () => throw new NotImplementedException()
                );
            var channels = chats.Chats.Choose(chat => chat.Match(_: () => None, channelTag: Some));

            var sns = channels
                .Find(channel => channel.Username == "startupneversleeps")
                .IfNone(() => throw new Exception("The channel is not found"));
            var snsPhoto = sns.Photo.Match(
                tag: identity,
                emptyTag: _ => throw new Exception("The channel does not have a photo")
            );
            var snsBigPhotoFile = snsPhoto.PhotoBig.Match(
                tag: identity,
                unavailableTag: _ => throw new Exception("The channel photo is unavailable")
            );

            InputFileLocation ToInput(FileLocation.Tag location) =>
                (InputFileLocation) new InputFileLocation.Tag(
                    volumeId: location.VolumeId,
                    localId: location.LocalId,
                    secret: location.Secret
                );

            var photoLoc = ToInput(snsBigPhotoFile);
            var fileType = await tg.GetFileType(photoLoc);
            var fileTypeExt = fileType.Match(
                pngTag: _ => ".png",
                jpegTag: _ => ".jpeg",
                _: () => throw new NotImplementedException()
            );

            using (var fs = File.OpenWrite($"sns-photo{fileTypeExt}"))
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
            await tg.SendUploadedPhoto(
                peer: (InputPeer) new InputPeer.SelfTag(),
                file: tgPhoto,
                message: "Telega works"
            );
        }

        static async Task Main()
        {
            // it is disabled by default
            Internal.TgTrace.IsEnabled = false;

            var cfg = await ReadConfig();
            using (var tg = await TelegramClient.Connect(cfg.ApiId, cfg.ApiHash))
            {
                await EnsureAuthenticated(tg, cfg);

                await SendOnePavelDurovPictureToMeExample(tg);
            }
        }
    }
}
