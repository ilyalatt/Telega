using System.Threading.Tasks;
using LanguageExt;
using Telega.Connect;
using Telega.Rpc.Dto.Functions.Channels;
using Telega.Rpc.Dto.Types;

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
    }
}