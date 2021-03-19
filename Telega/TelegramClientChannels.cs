using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LanguageExt;
using Telega.Connect;
using Telega.Rpc.Dto.Functions.Channels;
using Telega.Rpc.Dto.Types;
using Telega.Rpc.Dto.Types.Messages;

namespace Telega
{
    public class TelegramClientChannels
    {
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
        /// This method can be used for getting messages by Id (including albums)
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="messageId"></param>
        /// <returns>List of tags associated with this mesageId</returns>
        public async Task<Arr<Message.Tag>> GetMessageById(
            Some<InputChannel> channel,
            int messageId
        )
        {
            Messages messagesResponse =
                await GetMessages(channel: channel,
                    //We have to get 10 messages around required Id because of grouped messages (albums)
                    messages: Enumerable.Range(start: -10, count: 20)
                        .Select(selector: x => (InputMessage) new InputMessage.IdTag(id: messageId + x))
                        .ToArr()
                );
            
            List<Message.Tag> tags =
                messagesResponse
                    .AsChannelTag().Head()
                    .Messages
                    .Choose(Message.AsTag)
                    .ToList();

            Message.Tag mainMessage = tags.First(x => x.Id == messageId);

            return mainMessage.GroupedId
                    .Match(Some: groupedId =>
                            tags.Where( tag => tag.GroupedId == groupedId).OrderBy(x => x.Id).ToArr(),
                        None: () => { return new[] {mainMessage}; })
                ;
        }
    }
}