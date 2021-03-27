using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Telega.Client;
using Telega.Rpc.Dto.Types;
using Telega.Utils;

namespace Telega.Playground.Snippets {
    static class DownloadFirstChannelPicture {
        public static async Task Run(TelegramClient tg) {
            var chatsType = await tg.Messages.GetDialogs();
            var chats = chatsType.Default!;
            var channels = chats.Chats.NChoose(x => x.Channel);

            var firstChannel = channels.FirstOrDefault() ?? throw new Exception("A channel is not found.");
            var photo = firstChannel.Photo
                .Default ?? throw new Exception("The first channel does not have a photo.");
            var bigPhotoFile = photo.PhotoBig;

            var photoLoc = new InputFileLocation.PeerPhotoTag(
                peer: new InputPeer.ChannelTag(firstChannel.Id, firstChannel.AccessHash!.Value),
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

            await using var fs = File.OpenWrite($"channel-photo{fileTypeExt}");
            await tg.Upload.DownloadFile(fs, photoLoc);
        }
    }
}