using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using LanguageExt;
using Newtonsoft.Json;
using Telega.Rpc.Dto.Types;
using static LanguageExt.Prelude;

namespace Telega.Example
{
    static class Exts
    {
        public static T AssertSome<T>(this Option<T> opt) =>
            opt.IfNone(() => throw new ApplicationException("Should be Some, but got None."));
    }

    static class Program
    {
        static async Task SignInViaCode(TelegramClient tg, Config cfg)
        {
            var codeHash = await tg.Auth.SendCode(cfg.ApiHash, cfg.Phone);

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
            if (tg.Auth.IsAuthorized)
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

            var firstChannel = channels
                .HeadOrNone()
                .IfNone(() => throw new Exception("A channel is not found"));
            var photo = firstChannel.Photo
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
            var fileType = await tg.Upload.GetFileType(photoLoc);
            var fileTypeExt = fileType.Match(
                pngTag: _ => ".png",
                jpegTag: _ => ".jpeg",
                _: () => throw new NotImplementedException()
            );

            using (var fs = File.OpenWrite($"channel-photo{fileTypeExt}"))
            {
                await tg.Upload.DownloadFile(fs, photoLoc);
            }
        }

        static async Task PrintFirstChannelTop100MessagesExample(TelegramClient tg)
        {
            var chatsType = await tg.Messages.GetDialogs();
            var chats = chatsType.AsTag().AssertSome();
            var channels = chats.Chats.Choose(Chat.AsChannelTag);

            var firstChannel = channels
                .HeadOrNone()
                .IfNone(() => throw new Exception("A channel is not found"));

            var inputPeer = new InputPeer.ChannelTag(
                channelId: firstChannel.Id,
                accessHash: firstChannel.AccessHash.AssertSome()
            );
            var top100Messages = await tg.Messages.GetHistory(inputPeer, limit: 100);
            top100Messages.AsChannelTag().AssertSome().Messages.Iter(msg =>
            {
                Console.WriteLine(msg);
                Console.WriteLine();
            });
        }

        static async Task SendOnePavelDurovPictureToMeExample(TelegramClient tg)
        {
            const string photoUrl = "https://cdn1.img.jp.sputniknews.com/images/406/99/4069980.png";
            var photoName = Path.GetFileName(photoUrl);
            var photo = new WebClient().DownloadData(photoUrl);

            var tgPhoto = await tg.Upload.UploadFile(photoName, photo.Length, new MemoryStream(photo));
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
            using (var tg = await TelegramClient.Connect(cfg.ApiId))
            {
                await EnsureAuthorized(tg, cfg);

                // await DownloadFirstChannelPictureExample(tg);
                // await PrintFirstChannelTop100MessagesExample(tg);
                await SendOnePavelDurovPictureToMeExample(tg);
            }
        }
    }
}
