using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telega.Connect;
using Telega.Rpc.Dto.Functions.Messages;
using Telega.Rpc.Dto.Types;
using Telega.Rpc.Dto.Types.Messages;
using Telega.Utils;

namespace Telega.Client {
    public sealed class TelegramClientMessages {
        readonly TgBellhop _tg;
        internal TelegramClientMessages(TgBellhop tg) => _tg = tg;


        public async Task<Dialogs> GetDialogs() =>
            await _tg.Call(new GetDialogs(
                offsetDate: 0,
                offsetPeer: new InputPeer.SelfTag(),
                limit: 100,
                excludePinned: false,
                offsetId: 0,
                hash: 0,
                folderId: null
            )).ConfigureAwait(false);

        public async Task<Messages> GetHistory(
            InputPeer peer,
            int offsetId = 0,
            int offsetDate = 0,
            int addOffset = 0,
            int limit = 100,
            int maxId = 0,
            int minId = 0,
            int hash = 0
        ) =>
            await _tg.Call(new GetHistory(
                peer,
                offsetId,
                offsetDate,
                addOffset,
                limit,
                maxId,
                minId,
                hash
            )).ConfigureAwait(false);

        public async Task<UpdatesType> SendMessage(
            InputPeer peer,
            string message,
            int? scheduleDate = default
        ) =>
            await _tg.Call(new SendMessage(
                peer: peer,
                message: message,
                randomId: Rnd.NextInt64(),
                noWebpage: true,
                silent: false,
                background: false,
                clearDraft: false,
                replyToMsgId: null,
                replyMarkup: null,
                entities: null,
                scheduleDate: scheduleDate
            )).ConfigureAwait(false);

        public async Task<UpdatesType> SendPhoto(
            InputPeer peer,
            InputFile file,
            string message, 
            int? scheduleDate = default
        ) =>
            await _tg.Call(new SendMedia(
                randomId: Rnd.NextInt64(),
                background: false,
                clearDraft: false,
                media: new InputMedia.UploadedPhotoTag(
                    file: file,
                    stickers: null,
                    ttlSeconds: null
                ),
                peer: peer,
                entities: null,
                replyToMsgId: null,
                replyMarkup: null,
                message: message,
                silent: false,
                scheduleDate: scheduleDate
            )).ConfigureAwait(false);

        public async Task<UpdatesType> SendDocument(
            InputPeer peer,
            InputFile file,
            string mimeType,
            string message,
            IReadOnlyList<DocumentAttribute>? attributes = default,
            int? scheduleDate = default
        ) =>
            await _tg.Call(new SendMedia(
                randomId: Rnd.NextInt64(),
                background: false,
                clearDraft: false,
                media: new InputMedia.UploadedDocumentTag(
                    nosoundVideo: false,
                    forceFile: true,
                    file: file,
                    mimeType: mimeType,
                    attributes: attributes ?? new DocumentAttribute[0],
                    thumb: null,
                    stickers: null,
                    ttlSeconds: null
                ),
                peer: peer,
                silent: false,
                replyToMsgId: null,
                replyMarkup: null,
                entities: null,
                message: message,
                scheduleDate: scheduleDate
            )).ConfigureAwait(false);

        public async Task<UpdatesType> SendMedia(
            InputPeer peer,
            MessageMedia file,
            string message,
            int? scheduleDate = null
        ) =>
            await _tg.Call(new SendMedia(
                randomId: Rnd.NextInt64(),
                background: false,
                clearDraft: false,
                media: file.Match<InputMedia>(
                    _: () => throw new NotImplementedException(),
                    photoTag: photoTag => {
                        var photoContainer = photoTag.Photo ?? throw new TgInternalException("Unable to get photo", null); 
                        var photo = photoContainer.Default ?? throw new TgInternalException("Unable to get photo tag", null);
                        return new InputMedia.PhotoTag(
                            id: new InputPhoto.DefaultTag(
                                id: photo.Id,
                                accessHash: photo.AccessHash,
                                fileReference: photo.FileReference
                            ),
                            ttlSeconds: null
                        );
                    },
                    documentTag: documentTag => {
                        var documentContainer = documentTag.Document ?? throw new TgInternalException("Unable to get document", null);
                        var document = documentContainer .Default ?? throw new TgInternalException("Unable to get document tag", null);
                        return
                            new InputMedia.DocumentTag(
                                id: new InputDocument.DefaultTag(
                                    id: document.Id,
                                    accessHash: document.AccessHash,
                                    fileReference: document.FileReference
                                ),
                                ttlSeconds: null,
                                query: null
                            );
                    }
                ),
                peer: peer,
                silent: false,
                replyToMsgId: null,
                replyMarkup: null,
                entities: null,
                message: message,
                scheduleDate: scheduleDate
            )).ConfigureAwait(false);

        public async Task<UpdatesType> SendMultimedia(
            InputPeer peer,
            IReadOnlyList<MessageMedia> attachments,
            string? message = null,
            int? scheduleDate = null
        ) =>
            await _tg.Call(new SendMultiMedia(
                silent: false,
                background: false,
                clearDraft: false,
                peer: peer,
                replyToMsgId: null,
                multiMedia: attachments.NChoose((x, i) =>
                    x.Match(
                        _: () => throw new NotImplementedException(),
                        photoTag: photoTag => {
                            var photoContainer = photoTag.Photo ?? throw new TgInternalException("Unable to get photo", null);
                            var photo = photoContainer.Default ?? throw new TgInternalException("Unable to get photo tag", null);
                            return new InputSingleMedia(
                                media: new InputMedia.PhotoTag(
                                    id: new InputPhoto.DefaultTag(
                                        id: photo.Id,
                                        accessHash: photo.AccessHash,
                                        fileReference: photo.FileReference
                                    ),
                                    ttlSeconds: null
                                ),
                                randomId: Rnd.NextInt64(),
                                message: i == 0 ? message ?? string.Empty : string.Empty,
                                entities: null
                            );
                        },
                        documentTag: documentTag => {
                            var documentContainer = documentTag.Document ?? throw new TgInternalException("Unable to get document", null);
                            var document = documentContainer.Default ?? throw new TgInternalException("Unable to get document tag", null);
                            return new InputSingleMedia(
                                media: new InputMedia.DocumentTag(
                                    id: new InputDocument.DefaultTag(
                                        id: document.Id,
                                        accessHash: document.AccessHash,
                                        fileReference: document.FileReference
                                    ),
                                    ttlSeconds: null,
                                    query: null
                                ),
                                randomId: Rnd.NextInt64(),
                                message: i == 0 ? message ?? string.Empty : string.Empty,
                                entities: null
                            );
                        }
                    )
                ).ToList(),
                scheduleDate: scheduleDate
            )).ConfigureAwait(false);

        public async Task<bool> SendTyping(InputPeer peer) =>
            await _tg.Call(new SetTyping(
                action: new SendMessageAction.TypingTag(),
                peer: peer,
                topMsgId: null
            )).ConfigureAwait(false);

        public async Task<MessageMedia> UploadMediaAsPhoto(
            InputPeer peer,
            InputFile file
        ) =>
            await _tg.Call(new UploadMedia(
                peer: peer,
                media: new InputMedia.UploadedPhotoTag(
                    file: file,
                    stickers: null,
                    ttlSeconds: null
                )
            )).ConfigureAwait(false);

        public async Task<MessageMedia> UploadMediaAsDocument(
            InputPeer peer,
            InputFile file,
            string mimeType,
            IReadOnlyList<DocumentAttribute>? attributes = default
        ) =>
            await _tg.Call(new UploadMedia(
                peer: peer,
                media: new InputMedia.UploadedDocumentTag(
                    nosoundVideo: false,
                    file: file,
                    mimeType: mimeType,
                    attributes: attributes ?? new DocumentAttribute[0],
                    thumb: null,
                    stickers: null,
                    ttlSeconds: null,
                    forceFile: false
                )
            )).ConfigureAwait(false);

        public async Task<MessageMedia> UploadMediaAsDocument(
            InputPeer peer,
            string url
        ) =>
            await _tg.Call(new UploadMedia(
                peer: peer,
                media: new InputMedia.DocumentExternalTag(
                    url,
                    null
                )
            )).ConfigureAwait(false);
    }
}