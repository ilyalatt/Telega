using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Telega.Connect
{
    public delegate Task<TcpClient> TcpClientConnectionHandler(IPEndPoint endpoint);
}
