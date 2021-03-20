using System.Threading.Tasks;
using LanguageExt;
using Telega.Connect;
using Telega.Rpc.Dto.Functions.Channels;
using Telega.Rpc.Dto.Types;
using Telega.Rpc.Dto.Types.Messages;
using static LanguageExt.Prelude;

namespace Telega.Client {
    public sealed class TelegramClientChannels {
        readonly TgBellhop _tg;
        internal TelegramClientChannels(Some<TgBellhop> tg) => _tg = tg;

        public async Task<UpdatesType> JoinChannel(
            Some<InputChannel> channel
        ) =>
            await _tg.Call(new JoinChannel(
                channel: channel
            ));

        public async Task<Messages> GetMessages(
            Some<InputChannel> channel,
            Arr<InputMessage> messages
        ) =>
            await _tg.Call(new GetMessages(
                channel: channel,
                id: messages
            ));

        /// <summary>
        /// Get grouped messages (including albums)
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="messageId"></param>
        /// <returns>Grouped messages</returns>
        public async Task<Arr<Message.Tag>> GetGroupedMessages(
            Some<InputChannel> channel,
            int messageId
        ) {
            const int idRadius = 10;
            var messageIds = Range(@from: -idRadius, count: idRadius * 2 + 1)
               .Map(x => (InputMessage) new InputMessage.IdTag(id: messageId + x))
               .ToArr();

            var messagesResponse = await GetMessages(channel, messageIds);
            var messages = messagesResponse
               .AsChannelTag()
               .Bind(x => x.Messages)
               .Choose(Message.AsTag)
               .ToArr();

            var mainMessage = messages.Find(x => x.Id == messageId);
            var groupId = mainMessage.Bind(x => x.GroupedId);
            return groupId.Match(
                Some: _ => messages.Filter(x => x.GroupedId == groupId),
                None: () => mainMessage.ToArr()
            );
        }
    }
}