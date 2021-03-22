using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using LanguageExt;
using Telega.Client;
using Telega.Rpc.Dto.Functions.Contacts;
using Telega.Rpc.Dto.Functions.Messages;
using Telega.Rpc.Dto.Functions.Users;
using Telega.Rpc.Dto.Types;
using static LanguageExt.Prelude;
using Telega.Utils;

namespace Telega.Playground {
    static class Exts {
        public static T AssertSome<T>(this Option<T> opt) =>
            opt.IfNone(() => throw new ApplicationException("Should be Some, but got None."));
    }

    static class Program {
        static async Task DownloadFirstChannelPictureExample(TelegramClient tg) {
            var chatsType = await tg.Messages.GetDialogs();
            var chats = chatsType.Default!;
            var channels = chats.Chats.NChoose(x => x.Channel);

            var firstChannel = channels
               .HeadOrNone()
               .IfNone(() => throw new Exception("A channel is not found"));
            var photo = firstChannel.Photo
                .Default ?? throw new Exception("The first channel does not have a photo");
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

            using var fs = File.OpenWrite($"channel-photo{fileTypeExt}");
            await tg.Upload.DownloadFile(fs, photoLoc);
        }

        static async Task DownloadLastMovieFromSavedMessagesExample(TelegramClient tg) {
            var fullUserInfo = await tg.Call(new GetFullUser(new InputUser.SelfTag()));
            var userInfo = fullUserInfo.User.Default!;

            var chatPeer = (InputPeer) new InputPeer.UserTag(
                userId: userInfo.Id,
                accessHash: userInfo.AccessHash.AssertSome()
            );
            const int batchLimit = 100;

            async Task<IEnumerable<Document.DefaultTag>> GetHistory(int offset = 0) {
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
                var messages = resp.Slice!.Messages;
                var docs = messages
                   .Reverse()
                   .NChoose(x => x.Default)
                   .Choose(message => message.Media)
                   .NChoose(x => x.Document)
                   .Choose(x => x.Document)
                   .NChoose(x => x.Default);
                return messages.Count == 0
                    ? docs
                    : (await GetHistory(offset + batchLimit)).Concat(docs);
            }

            var history = await GetHistory();
            var video = history.Last(x => x.Attributes.NChoose(x => x.Video).Any());
            var videoName = video.Attributes.NChoose(x => x.Filename).Single().FileName;

            var videoLocation = new InputFileLocation.EncryptedTag(
                id: video.Id,
                accessHash: video.AccessHash
            );
            using var fs = File.OpenWrite(videoName);
            await tg.Upload.DownloadFile(fs, videoLocation);
        }

        static async Task PrintFirstChannelTop100MessagesExample(TelegramClient tg) {
            var chatsType = await tg.Messages.GetDialogs();
            var chats = chatsType.Default!;
            var channels = chats.Chats.NChoose(x => x.Channel);

            var firstChannel = channels
               .HeadOrNone()
               .IfNone(() => throw new Exception("A channel is not found"));

            var inputPeer = new InputPeer.ChannelTag(
                channelId: firstChannel.Id,
                accessHash: firstChannel.AccessHash.AssertSome()
            );
            var top100Messages = await tg.Messages.GetHistory(inputPeer, limit: 100);
            top100Messages.Channel!.Messages.Iter(msg => {
                Console.WriteLine(msg);
                Console.WriteLine();
            });
        }

        static async Task SendOnePavelDurovPictureToMeExample(TelegramClient tg) {
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

        static async Task SendMultiMedia(TelegramClient tg) {
            async Task<MessageMedia> UploadPhoto(
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

            async Task<MessageMedia> UploadVideo(
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

            const string photoUrl = "https://cdn1.img.jp.sputniknews.com/images/406/99/4069980.png";
            var photoName = Path.GetFileName(path: photoUrl);
            var photo = new WebClient().DownloadData(address: photoUrl);

            const string videoUrl = "http://techslides.com/demos/sample-videos/small.mp4";
            var videoName = Path.GetFileName(path: videoUrl);
            var video = new WebClient().DownloadData(address: videoUrl);

            var inputPeer = new InputPeer.SelfTag();

            var sentImage = await UploadPhoto(photoName, photo, inputPeer);
            var sentVideo = await UploadVideo(videoName, video, inputPeer, "video/mp4");
            await tg.Messages.SendMultimedia(
                peer: inputPeer,
                message: "Sent from Telega",
                attachments: new[] {
                    sentImage,
                    sentVideo
                }
            );
        }


        static async Task PrintUserInfo(TelegramClient tg) {
            var myInfo = await tg.Call(new GetFullUser(new InputUser.SelfTag()));
            Console.WriteLine(myInfo);
        }

        static async Task ListenUpdates(TelegramClient tg) {
            Console.WriteLine("Listening to updates until exit.");
            tg.Updates.Stream.Subscribe(
                onNext: updatesType => {
                    var messageText = updatesType.Match(
                        updateShortMessageTag: x => Some("updateShortMessageTag: " + x.Message),
                        updateShortChatMessageTag: x => Some("updateShortChatMessageTag: " + x.Message),
                        updateShortTag: update => update.Update.Match(
                            newMessageTag: msg => msg.Message.Default.NMap(x => "newMessageTag: " + x.Message).Apply(Optional),
                            editMessageTag: msg => msg.Message.Default.NMap(x => "editMessageTag: " + x.Message).Apply(Optional),
                            editChannelMessageTag: msg =>
                                msg.Message.Default.NMap(x => "editChannelMessageTag: " + x.Message).Apply(Optional),
                            _: () => None
                        ),
                        _: () => None
                    );
                    messageText.Iter(Console.WriteLine);
                },
                onError: Console.WriteLine
            );

            tg.Updates.Stream.Subscribe(
                onNext: updatesType => {
                    var messageText = updatesType.Match(
                        updateShortMessageTag: x => Some("updateShortMessageTag: " + x.Message),
                        updateShortChatMessageTag: x => Some("updateShortChatMessageTag: " + x.Message),
                        updateShortTag: update => update.Update.Match(
                            newMessageTag: msg => msg.Message.Default.NMap(x => "newMessageTag: " + x.Message).Apply(Optional),
                            editMessageTag: msg => msg.Message.Default.NMap(x => "editMessageTag: " + x.Message).Apply(Optional),
                            editChannelMessageTag: msg =>
                                msg.Message.Default.NMap(x => "editChannelMessageTag: " + x.Message).Apply(Optional),
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

        static async Task<List<(int userIdx, User.DefaultTag user)>> ImportUsers(
            TelegramClient tg,
            IEnumerable<(string phone, string firstName, string lastName)> users
        ) {
            var resp = await tg.Call(new ImportContacts(
                contacts: users.Map((userIdx, user) => new InputContact(
                    clientId: userIdx,
                    phone: user.phone,
                    firstName: user.firstName,
                    lastName: user.lastName
                )).ToArr()
            ));
            var usersMap = resp.Users.NChoose(x => x.Default).ToDictionary(x => x.Id);
            return resp.Imported.Select(x => ((int) x.ClientId, usersMap[x.UserId])).ToList();
        }

        static async Task DownloadGroupImages(TelegramClient tg) {
            const string groupName = "Amsterdam";
            const string counterFormat = "000";

            var dialogs = await tg.Messages.GetDialogs();
            var chat = dialogs.Default!.Chats.NChoose(x => x.Default).Single(x => x.Title == groupName);
            var chatPeer = new InputPeer.ChatTag(chatId: chat.Id);

            const int batchLimit = 100;

            async Task<IEnumerable<Photo.DefaultTag>> GetHistory(int offset = 0) {
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
                var messages = resp.Slice!.Messages;
                var photos = messages
                   .Reverse()
                   .NChoose(x => x.Default)
                   .Choose(message => message.Media)
                   .NChoose(x => x.Photo)
                   .Choose(x => x.Photo)
                   .NChoose(x => x.Default);
                return messages.Count == 0
                    ? photos
                    : (await GetHistory(offset + batchLimit)).Concat(photos);
            }

            Console.WriteLine("Scraping chat messages");
            var allPhotos = (await GetHistory()).ToArr();

            const string photosDir = groupName;
            if (!Directory.Exists(photosDir)) {
                Directory.CreateDirectory(photosDir);
            }

            Console.WriteLine("Downloading images");
            var counter = 1;
            foreach (var photo in allPhotos) {
                var biggestSize = photo.Sizes.NChoose(x => x.Default).OrderByDescending(x => x.Size).First();
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

                using (var fileStream = File.OpenWrite(photoPath)) {
                    var ms = new MemoryStream();
                    await tg.Upload.DownloadFile(fileStream, photoFileLocation);
                }

                Console.WriteLine($"{counterStr}/{allPhotos.Count} downloaded");
            }
        }

        static async Task Main() {
            Console.WriteLine("Playground is launched.");
            Console.WriteLine("Connecting to Telegram.");
            using var tg = await TelegramClient.Connect(Authorizer.ApiId);
            await Authorizer.Authorize(tg);

            // await PrintUserInfo(tg);
            // await DownloadFirstChannelPictureExample(tg);
            // await PrintFirstChannelTop100MessagesExample(tg);
            // await SendOnePavelDurovPictureToMeExample(tg);
            await SendMultiMedia(tg);
            await ListenUpdates(tg);
        }
    }
}