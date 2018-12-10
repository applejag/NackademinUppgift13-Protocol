using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using BattleshipProtocol.Protocol;
using JetBrains.Annotations;

namespace BattleshipProtocol
{
    public class BattleGame
    {
        private readonly TcpClient _client;
        private readonly BattleStream _stream;

        private BattleGame(TcpClient client)
        {
            _client = client;
            _stream = new BattleStream(client.GetStream());
        }

        /// <summary>
        /// <para>
        /// Connects to a host at a given address. Supports both IPv4 and IPv6, given it is enabled on the OS.
        /// </para>
        /// <para>
        /// On connection error, use <see cref="SocketException.ErrorCode"/> from the thrown error to obtain the cause of the error.
        /// Refer to the <see href="https://docs.microsoft.com/en-us/windows/desktop/winsock/windows-sockets-error-codes-2">Windows Sockets version 2 API error code</see> documentation.
        /// </para>
        /// </summary>
        /// <param name="address">Host name or IP address.</param>
        /// <param name="port">Host port.</param>
        /// <exception cref="SocketException">An error occurred when accessing the socket.</exception>
        /// <exception cref="ArgumentNullException">The <paramref name="address"/> is not null.</exception>
        [NotNull]
        public static async Task<BattleGame> ConnectAsync([NotNull] string address, ushort port)
        {
            var tcp = new TcpClient();

            await tcp.ConnectAsync(address, port);

            return new BattleGame(tcp);
        }

        public static string HelloWorld()
        {
            return "Hello World!";
        }

        public virtual void Dispose()
        {
            _client.Dispose();
            _stream.Dispose();
        }
    }
}