using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telega.Connect;
using Telega.Rpc.Dto.Functions.Channels;
using Telega.Rpc.Dto.Types;
using Telega.Rpc.Dto.Types.Messages;
using Telega.Utils;

namespace Telega.Client {
    public sealed class TelegramClientChannels {
        readonly TgBellhop _tg;
        internal TelegramClientChannels(TgBellhop tg) => _tg = tg;

        public async Task<UpdatesType> JoinChannel(
            InputChannel channel
        ) =>
            await _tg.Call(new JoinChannel(
                channel: channel
            )).ConfigureAwait(false);

        public async Task<Messages> GetMessages(
            InputChannel channel,
            IReadOnlyList<InputMessage> messages
        ) =>
            await _tg.Call(new GetMessages(
                channel: channel,
                id: messages
            )).ConfigureAwait(false);

        /// <summary>
        /// Get grouped messages (including albums)
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="messageId"></param>
        /// <returns>Grouped messages</returns>
        public async Task<IReadOnlyList<Message.DefaultTag>> GetGroupedMessages(
            InputChannel channel,
            int messageId
        ) {
            const int idRadius = 10;
            var messageIds = Enumerable.Range(start: -idRadius, count: idRadius * 2 + 1)
               .Select(x => (InputMessage) new InputMessage.IdTag(id: messageId + x))
               .ToList();

            var messagesResponse = await GetMessages(channel, messageIds).ConfigureAwait(false);
            var messages = messagesResponse.Channel?.Messages.NChoose(x => x.Default).ToList();
            if (messages == null) {
                return new Message.DefaultTag[0];
            }

            var mainMessage = messages.FirstOrDefault(x => x.Id == messageId);
            var groupId = mainMessage?.GroupedId;
            return groupId != null
                ? messages.Where(x => x.GroupedId == groupId).ToList()
                : new[] { mainMessage! };
        }
    }
}