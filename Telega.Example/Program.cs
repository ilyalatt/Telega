using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using LanguageExt;
using Newtonsoft.Json;
using Telega.Rpc.Dto.Functions.Contacts;
using Telega.Rpc.Dto.Functions.Messages;
using Telega.Rpc.Dto.Functions.Users;
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
            var codeHash = await tg.Auth.SendCode(cfg.ApiHash!, cfg.Phone!);

            while (true)
            {
                try
                {
                    Console.WriteLine("Enter the telegram code");
                    var code = Console.ReadLine();
                    await tg.Auth.SignIn(cfg.Phone!, codeHash, code!);
                    break;
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
            await tg.Auth.CheckPassword(pwdInfo, cfg.Password!);
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
            var bigPhotoFile = photo.PhotoBig;

            var photoLoc = new InputFileLocation.PeerPhotoTag(
                peer: new InputPeer.ChannelTag(firstChannel.Id, firstChannel.AccessHash.AssertSome()),
                volumeId: bigPhotoFile.VolumeId,
                localId: bigPhotoFile.LocalId,
                big: true
            );
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

        static async Task DownloadLastMovieFromSavedMessagesExample(TelegramClient tg)
        {
            var fullUserInfo = await tg.Call(new GetFullUser(new InputUser.SelfTag()));
            var userInfo = fullUserInfo.User.AsTag().AssertSome();

            var chatPeer = (InputPeer) new InputPeer.UserTag(
                userId: userInfo.Id,
                accessHash: userInfo.AccessHash.AssertSome()
            );
            const int batchLimit = 100;

            async Task<IEnumerable<Document.Tag>> GetHistory(int offset = 0)
            {
                var resp = await tg.Call(new GetHistory(
                    peer: chatPeer,
                    addOffset: offset,
                    limit: batchLimit,
                    minId: 0,
                    maxId: 0,
                    hash: 0,
                    offsetDate: 0,
                    offsetId: 0
                ));
                var messages = resp.AsSliceTag().AssertSome().Messages;
                var docs = messages
                    .Reverse()
                    .Choose(Message.AsTag)
                    .Choose(message => message.Media)
                    .Choose(MessageMedia.AsDocumentTag)
                    .Choose(x => x.Document)
                    .Choose(Document.AsTag);
                return messages.Count == 0
                    ? docs
                    : (await GetHistory(offset + batchLimit)).Concat(docs);
            }

            var history = await GetHistory();
            var video = history.Last(x => x.Attributes.Choose(DocumentAttribute.AsVideoTag).Any());
            var videoName = video.Attributes.Choose(DocumentAttribute.AsFilenameTag).Single().FileName;

            var videoLocation = new InputFileLocation.EncryptedTag(
                id: video.Id,
                accessHash: video.AccessHash
            );
            using (var fs = File.OpenWrite(videoName))
            {
                await tg.Upload.DownloadFile(fs, videoLocation);
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
                message: "Sent from Telega",
                scheduleDate: None
            );
        }

        static async Task SendMultiMedia(TelegramClient tg)
        {
            async Task<MessageMedia> UploadPhoto(string photoName, byte[] bytes, Some<InputPeer> peer)
            {
                InputFile? tgPhoto = await tg.Upload.UploadFile(name: photoName, fileLength: bytes.Length,
                    stream: new MemoryStream(buffer: bytes));

                MessageMedia? messageMedia = await tg.Messages.UploadMediaAsPhoto(
                    peer: peer,
                    file: tgPhoto);
                return messageMedia;
            }

            async Task<MessageMedia> UploadVideo(string videoName, byte[] bytes, Some<InputPeer> peer,
                Some<string> mimeType)
            {
                InputFile? tgPhoto = await tg.Upload.UploadFile(name: videoName, fileLength: bytes.Length,
                    stream: new MemoryStream(buffer: bytes));

                MessageMedia? messageMedia = await tg.Messages.UploadMediaAsDocument(
                    peer: peer,
                    file: tgPhoto,
                    mimeType,
                    Empty
                );
                return messageMedia;
            }


            const string photoUrl = "https://cdn1.img.jp.sputniknews.com/images/406/99/4069980.png";
            var photoName = Path.GetFileName(path: photoUrl);
            var photo = new WebClient().DownloadData(address: photoUrl);

            const string videoUrl = "http://techslides.com/demos/sample-videos/small.mp4";
            var videoName = Path.GetFileName(path: videoUrl);
            var video = new WebClient().DownloadData(address: videoUrl);

            var inputPeer = new InputPeer.SelfTag();

            MessageMedia sentImage = await UploadPhoto(photoName, photo, inputPeer);
            MessageMedia sentVideo = await UploadVideo(videoName, video, inputPeer, "video/mp4");

            await tg.Messages.SendMultimedia(
                peer: inputPeer,
                message: "Sent from Telega",
                attachments: new[]
                {
                    sentImage,
                    sentVideo,
                },
                scheduleDate: None);
        }


        static async Task PrintUserInfo(TelegramClient tg)
        {
            var myInfo = await tg.Call(new GetFullUser(new InputUser.SelfTag()));
            Console.WriteLine(myInfo);
        }

        static async Task ListenUpdates(TelegramClient tg)
        {
            tg.Updates.Stream.Subscribe(
                onNext: updatesType =>
                {
                    var messageText = updatesType.Match(
                        updateShortMessageTag: x => Some("updateShortMessageTag: " + x.Message),
                        updateShortChatMessageTag: x => Some("updateShortChatMessageTag: " + x.Message),
                        updateShortTag: update => update.Update.Match(
                            newMessageTag: msg => msg.Message.AsTag().Map(x => "newMessageTag: " + x.Message),
                            editMessageTag: msg => msg.Message.AsTag().Map(x => "editMessageTag: " + x.Message),
                            editChannelMessageTag: msg =>
                                msg.Message.AsTag().Map(x => "editChannelMessageTag: " + x.Message),
                            _: () => None
                        ),
                        _: () => None
                    );
                    messageText.Iter(Console.WriteLine);
                },
                onError: Console.WriteLine
            );

            tg.Updates.Stream.Subscribe(
                onNext: updatesType =>
                {
                    updatesType.AsUpdateShortTag().Bind(x => x.Update.AsNewMessageTag()).IfSome(msg => { });
                    var messageText = updatesType.Match(
                        updateShortMessageTag: x => Some("updateShortMessageTag: " + x.Message),
                        updateShortChatMessageTag: x => Some("updateShortChatMessageTag: " + x.Message),
                        updateShortTag: update => update.Update.Match(
                            newMessageTag: msg => msg.Message.AsTag().Map(x => "newMessageTag: " + x.Message),
                            editMessageTag: msg => msg.Message.AsTag().Map(x => "editMessageTag: " + x.Message),
                            editChannelMessageTag: msg =>
                                msg.Message.AsTag().Map(x => "editChannelMessageTag: " + x.Message),
                            _: () => None
                        ),
                        _: () => None
                    );
                    messageText.Iter(Console.WriteLine);
                },
                onError: Console.WriteLine
            );

            await Task.Delay(Timeout.Infinite);
        }

        static async Task<Arr<(int userIdx, User.Tag user)>> ImportUsers(
            TelegramClient tg,
            IEnumerable<(string phone, string firstName, string lastName)> users
        )
        {
            var resp = await tg.Call(new ImportContacts(
                contacts: users.Map((userIdx, user) => new InputContact(
                    clientId: userIdx,
                    phone: user.phone,
                    firstName: user.firstName,
                    lastName: user.lastName
                )).ToArr()
            ));
            var usersMap = resp.Users.Choose(User.AsTag).ToDictionary(x => x.Id);
            return resp.Imported.Map(x => ((int) x.ClientId, usersMap[x.UserId]));
        }

        static async Task DownloadGroupImages(TelegramClient tg)
        {
            const string groupName = "Amsterdam";
            const string counterFormat = "000";

            var dialogs = await tg.Messages.GetDialogs();
            var chat = dialogs.AsTag().AssertSome().Chats.Choose(Chat.AsTag).Single(x => x.Title == groupName);
            var chatPeer = new InputPeer.ChatTag(chatId: chat.Id);

            const int batchLimit = 100;

            async Task<IEnumerable<Photo.Tag>> GetHistory(int offset = 0)
            {
                var resp = await tg.Call(new GetHistory(
                    peer: chatPeer,
                    addOffset: offset,
                    limit: batchLimit,
                    minId: 0,
                    maxId: 0,
                    hash: 0,
                    offsetDate: 0,
                    offsetId: 0
                ));
                var messages = resp.AsSliceTag().AssertSome().Messages;
                var photos = messages
                    .Reverse()
                    .Choose(Message.AsTag)
                    .Choose(message => message.Media)
                    .Choose(MessageMedia.AsPhotoTag)
                    .Choose(x => x.Photo)
                    .Choose(Photo.AsTag);
                return messages.Count == 0
                    ? photos
                    : (await GetHistory(offset + batchLimit)).Concat(photos);
            }

            Console.WriteLine("Scraping chat messages");
            var allPhotos = (await GetHistory()).ToArr();

            const string photosDir = groupName;
            if (!Directory.Exists(photosDir)) Directory.CreateDirectory(photosDir);

            Console.WriteLine("Downloading images");
            var counter = 1;
            foreach (var photo in allPhotos)
            {
                var biggestSize = photo.Sizes.Choose(PhotoSize.AsTag).OrderByDescending(x => x.Size).First();
                var location = biggestSize.Location;

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

                using (var fileStream = File.OpenWrite(photoPath))
                {
                    var ms = new MemoryStream();
                    await tg.Upload.DownloadFile(fileStream, photoFileLocation);
                }

                Console.WriteLine($"{counterStr}/{allPhotos.Count} downloaded");
            }
        }

        static async Task Main()
        {
            // it is disabled by default
            Internal.TgTrace.IsEnabled = false;

            var cfg = await ReadConfig();
            using (var tg = await TelegramClient.Connect(cfg.ApiId))
            {
                await EnsureAuthorized(tg, cfg);

                // await PrintUserInfo(tg);
                // await DownloadFirstChannelPictureExample(tg);
                // await PrintFirstChannelTop100MessagesExample(tg);
                //await SendOnePavelDurovPictureToMeExample(tg);
                await SendMultiMedia(tg);
                await ListenUpdates(tg);
            }
        }
    }
}