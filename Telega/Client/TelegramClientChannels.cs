﻿using System.Linq;
using System.Threading.Tasks;
using LanguageExt;
using Telega.Connect;
using Telega.Rpc.Dto.Functions.Channels;
using Telega.Rpc.Dto.Types;
using Telega.Rpc.Dto.Types.Messages;
using static LanguageExt.Prelude;
using Telega.Utils;

namespace Telega.Client {
    public sealed class TelegramClientChannels {
        readonly TgBellhop _tg;
        internal TelegramClientChannels(Some<TgBellhop> tg) => _tg = tg;

        public async Task<UpdatesType> JoinChannel(
            Some<InputChannel> channel
        ) =>
            await _tg.Call(new JoinChannel(
                channel: channel
            )).ConfigureAwait(false);

        public async Task<Messages> GetMessages(
            Some<InputChannel> channel,
            Arr<InputMessage> messages
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
        public async Task<Arr<Message.DefaultTag>> GetGroupedMessages(
            Some<InputChannel> channel,
            int messageId
        ) {
            const int idRadius = 10;
            var messageIds = Range(@from: -idRadius, count: idRadius * 2 + 1)
               .Map(x => (InputMessage) new InputMessage.IdTag(id: messageId + x))
               .ToArr();

            var messagesResponse = await GetMessages(channel, messageIds).ConfigureAwait(false);
            var messages = messagesResponse.Channel?.Messages.NChoose(x => x.Default).ToList();
            if (messages == null) {
                return Empty;
            }

            var mainMessage = messages.FirstOrDefault(x => x.Id == messageId);
            var groupId = mainMessage?.GroupedId;
            return groupId.Match(
                Some: _ => messages.Where(x => x.GroupedId == groupId).ToArray(),
                None: () => Array(mainMessage!)
            );
        }
    }
}