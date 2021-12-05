using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NullExtensions;
using Telega.Client;
using Telega.Rpc.Dto.Types;

namespace Telega.Playground.Snippets {
    static class DownloadFirstChannelPicture {
        public static async Task Run(TelegramClient tg) {
            var chatsType = await tg.Messages.GetDialogs();
            var chats = chatsType.Default!;
            var channels = chats.Chats.NChoose(x => x.Channel);

            var firstChannel = channels.FirstOrDefault() ?? throw new Exception("A channel is not found.");
            var photo = firstChannel.Photo
                .Default ?? throw new Exception("The first channel does not have a photo.");

            var photoLoc = new InputFileLocation.PeerPhotoTag(
                peer: new InputPeer.ChannelTag(firstChannel.Id, firstChannel.AccessHash!.Value),
                big: true,
                photoId: photo.PhotoId
            );
            var fileType = await tg.Upload.GetFileType(photoLoc);
            var fileTypeExt = fileType.Match(
                pngTag: _ => ".png",
                jpegTag: _ => ".jpeg",
                _: () => throw new NotImplementedException()
            );

            await using var fs = File.OpenWrite($"channel-photo{fileTypeExt}");
            await tg.Upload.DownloadFile(fs, photoLoc);
        }
    }
}