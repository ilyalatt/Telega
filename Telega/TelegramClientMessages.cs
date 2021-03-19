using System;
using System.Threading.Tasks;
using LanguageExt;
using Telega.Connect;
using Telega.Rpc.Dto.Functions.Messages;
using Telega.Rpc.Dto.Types;
using Telega.Rpc.Dto.Types.Messages;
using Telega.Utils;
using static LanguageExt.Prelude;

namespace Telega {
    public sealed class TelegramClientMessages {
        readonly TgBellhop _tg;
        internal TelegramClientMessages(Some<TgBellhop> tg) => _tg = tg;


        public async Task<Dialogs> GetDialogs() =>
            await _tg.Call(new GetDialogs(
                offsetDate: 0,
                offsetPeer: new InputPeer.SelfTag(),
                limit: 100,
                excludePinned: false,
                offsetId: 0,
                hash: 0,
                folderId: None
            ));

        public async Task<Messages> GetHistory(
            Some<InputPeer> peer,
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
            ));

        public async Task<UpdatesType> SendMessage(
            Some<InputPeer> peer,
            Some<string> message,
            Option<int> scheduleDate = default
        ) =>
            await _tg.Call(new SendMessage(
                peer: peer,
                message: message,
                randomId: Rnd.NextInt64(),
                noWebpage: true,
                silent: false,
                background: false,
                clearDraft: false,
                replyToMsgId: None,
                replyMarkup: None,
                entities: None,
                scheduleDate: scheduleDate
            ));

        public async Task<UpdatesType> SendPhoto(
            Some<InputPeer> peer,
            Some<InputFile> file,
            Some<string> message,
            Option<int> scheduleDate = default
        ) =>
            await _tg.Call(new SendMedia(
                randomId: Rnd.NextInt64(),
                background: false,
                clearDraft: false,
                media: new InputMedia.UploadedPhotoTag(
                    file: file,
                    stickers: None,
                    ttlSeconds: None
                ),
                peer: peer,
                entities: None,
                replyToMsgId: None,
                replyMarkup: None,
                message: message,
                silent: false,
                scheduleDate: scheduleDate
            ));

        public async Task<UpdatesType> SendDocument(
            Some<InputPeer> peer,
            Some<InputFile> file,
            Some<string> mimeType,
            Some<string> message,
            Arr<DocumentAttribute> attributes = default,
            Option<int> scheduleDate = default
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
                    attributes: attributes,
                    thumb: None,
                    stickers: None,
                    ttlSeconds: None
                ),
                peer: peer,
                silent: false,
                replyToMsgId: None,
                replyMarkup: None,
                entities: None,
                message: message,
                scheduleDate: scheduleDate
            ));

        public async Task<UpdatesType> SendMedia(
            Some<InputPeer> peer,
            Some<MessageMedia> file,
            Some<string> message,
            Option<int> scheduleDate = default
        ) =>
            await _tg.Call(new SendMedia(
                randomId: Rnd.NextInt64(),
                background: false,
                clearDraft: false,
                media: file.Head().Match<InputMedia>(
                    _: () => throw new NotImplementedException(),
                    photoTag: photoTag => {
                        var photo = photoTag.Photo
                           .HeadOrNone()
                           .IfNone(() => throw new TgInternalException("Unable to get photo", None))
                           .AsTag()
                           .HeadOrNone()
                           .IfNone(() => throw new TgInternalException("Unable to get photo tag", None));
                        return new InputMedia.PhotoTag(
                            id: new InputPhoto.Tag(
                                id: photo.Id,
                                accessHash: photo.AccessHash,
                                fileReference: photo.FileReference
                            ),
                            ttlSeconds: None
                        );
                    },
                    documentTag: documentTag => {
                        var document = documentTag.Document
                           .HeadOrNone()
                           .IfNone(() => throw new TgInternalException("Unable to get document", None))
                           .AsTag()
                           .HeadOrNone()
                           .IfNone(() => throw new TgInternalException("Unable to get document tag", None));
                        return
                            new InputMedia.DocumentTag(
                                id: new InputDocument.Tag(
                                    id: document.Id,
                                    accessHash: document.AccessHash,
                                    fileReference: document.FileReference
                                ),
                                ttlSeconds: None,
                                query: None
                            );
                    }
                ),
                peer: peer,
                silent: false,
                replyToMsgId: None,
                replyMarkup: None,
                entities: None,
                message: message,
                scheduleDate: scheduleDate
            ));

        public async Task<UpdatesType> SendMultimedia(
            Some<InputPeer> peer,
            Arr<MessageMedia> attachments,
            Option<string> message = default,
            Option<int> scheduleDate = default
        ) =>
            await _tg.Call(new SendMultiMedia(
                silent: false,
                background: false,
                clearDraft: false,
                peer: peer,
                replyToMsgId: None,
                multiMedia: new Arr<InputSingleMedia>(
                    attachments.Choose<MessageMedia, InputSingleMedia>((i, x) =>
                        x.Match(
                            _: () => throw new NotImplementedException(),
                            photoTag: photoTag => {
                                var photo = photoTag.Photo
                                   .HeadOrNone()
                                   .IfNone(() => throw new TgInternalException("Unable to get photo", None))
                                   .AsTag()
                                   .HeadOrNone()
                                   .IfNone(() => throw new TgInternalException("Unable to get photo tag", None));
                                return new InputSingleMedia(
                                    media: new InputMedia.PhotoTag(
                                        id: new InputPhoto.Tag(
                                            id: photo.Id,
                                            accessHash: photo.AccessHash,
                                            fileReference: photo.FileReference
                                        ),
                                        ttlSeconds: None
                                    ),
                                    randomId: Rnd.NextInt64(),
                                    message: i == 0 ? message.IfNone(string.Empty) : string.Empty,
                                    entities: None
                                );
                            },
                            documentTag: documentTag => {
                                var document = documentTag.Document
                                   .HeadOrNone()
                                   .IfNone(() => throw new TgInternalException("Unable to get document", None))
                                   .AsTag()
                                   .HeadOrNone()
                                   .IfNone(() => throw new TgInternalException("Unable to get document tag", None));
                                return new InputSingleMedia(
                                    media: new InputMedia.DocumentTag(
                                        id: new InputDocument.Tag(
                                            id: document.Id,
                                            accessHash: document.AccessHash,
                                            fileReference: document.FileReference
                                        ),
                                        ttlSeconds: None,
                                        query: None
                                    ),
                                    randomId: Rnd.NextInt64(),
                                    message: i == 0 ? message.IfNone(string.Empty) : string.Empty,
                                    entities: None
                                );
                            }
                        )
                    )),
                scheduleDate: scheduleDate
            ));

        public async Task<bool> SendTyping(Some<InputPeer> peer) =>
            await _tg.Call(new SetTyping(
                action: new SendMessageAction.TypingTag(),
                peer: peer,
                topMsgId: None
            ));

        public async Task<MessageMedia> UploadMediaAsPhoto(
            Some<InputPeer> peer,
            Some<InputFile> file
        ) =>
            await _tg.Call(new UploadMedia(
                peer: peer,
                media: new InputMedia.UploadedPhotoTag(
                    file: file,
                    stickers: None,
                    ttlSeconds: None
                )
            ));

        public async Task<MessageMedia> UploadMediaAsDocument(
            Some<InputPeer> peer,
            Some<InputFile> file,
            Some<string> mimeType,
            Arr<DocumentAttribute> attributes = default
        ) =>
            await _tg.Call(new UploadMedia(
                peer: peer,
                media: new InputMedia.UploadedDocumentTag(
                    nosoundVideo: false,
                    file: file,
                    mimeType: mimeType,
                    attributes: attributes,
                    thumb: None,
                    stickers: None,
                    ttlSeconds: None,
                    forceFile: false
                )
            ));

        public async Task<MessageMedia> UploadMediaAsDocument(
            Some<InputPeer> peer,
            Some<string> url
        ) =>
            await _tg.Call(new UploadMedia(
                peer: peer,
                media: new InputMedia.DocumentExternalTag(
                    url,
                    None
                )
            ));
    }
}