using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BattleshipProtocol.Protocol
{
    public class BattleClient : IDisposable
    {
        private TcpClient _client;

        private BattleClient(TcpClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Connects to a host at a given address.
        /// </summary>
        /// <param name="address">Host name or IP address.</param>
        /// <param name="port">Host port.</param>
        [NotNull]
        public static async Task<BattleClient> ConnectAsync([NotNull] string address, ushort port)
        {
            var tcp = new TcpClient();

            await tcp.ConnectAsync(address, port);

            return new BattleClient(tcp);
        }

        public virtual void Dispose()
        {
            _client?.Dispose();
            _client = null;
        }
    }
}
