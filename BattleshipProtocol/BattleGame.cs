using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using BattleshipProtocol.Game.Commands;
using BattleshipProtocol.Protocol;
using BattleshipProtocol.Protocol.Exceptions;
using BattleshipProtocol.Protocol.Internal;
using BattleshipProtocol.Protocol.Internal.Extensions;
using JetBrains.Annotations;

namespace BattleshipProtocol
{
    public class BattleGame : IDisposable
    {
        public const string ProtocolVersion = "BATTLESHIP/1.0";

        private readonly TcpClient _client;
        private readonly StreamConnection _connection;

        public bool IsHost { get; }
        public string PlayerName { get; }
        public string OpponentName { get; private set; }

        public PacketService PacketService { get; }

        public EndPoint RemoteEndPoint => _client.Client.RemoteEndPoint;

        private BattleGame(TcpClient client, StreamConnection connection, PacketService packetService, string playerName, bool isHost)
        {
            IsHost = isHost;
            _client = client;
            _connection = connection;
            PacketService = packetService;

            PacketService.BeginListening();
        }

        /// <summary>
        /// <para>
        /// Connects to a host at a given address and completes the version handshake. Supports both IPv4 and IPv6, given it is enabled on the OS.
        /// </para>
        /// <para>
        /// On connection error, use <see cref="SocketException.ErrorCode"/> from the thrown error to obtain the cause of the error.
        /// Refer to the <see href="https://docs.microsoft.com/en-us/windows/desktop/winsock/windows-sockets-error-codes-2">Windows Sockets version 2 API error code</see> documentation.
        /// </para>
        /// <para>
        /// On packet error, use <see cref="ProtocolException.ErrorMessage"/> from the thrown error to obtain the cause of the error. 
        /// </para>
        /// </summary>
        /// <param name="address">Host name or IP address.</param>
        /// <param name="port">Host port.</param>
        /// <param name="playerName">The name of this player.</param>
        /// <param name="timeout">Timeout in milliseconds for awaiting version handshake.</param>
        /// <exception cref="SocketException">An error occurred when accessing the socket.</exception>
        /// <exception cref="ArgumentNullException">The <paramref name="address"/> is not null.</exception>
        /// <exception cref="ProtocolException">Version handshake failed.</exception>
        [NotNull]
        public static async Task<BattleGame> ConnectAsync([NotNull] string address, ushort port, 
            [NotNull] string playerName, int timeout = 10_000)
        {
            var tcp = new TcpClient();

            await tcp.ConnectAsync(address, port);
            var connection = new StreamConnection(tcp.GetStream());
            var packetService = new PacketService(connection);

            await packetService.EnsureVersionGreeting(ProtocolVersion, timeout);

            packetService.RegisterCommand(new FireCommand());
            packetService.RegisterCommand(new HelloCommand());
            packetService.RegisterCommand(new HelpCommand());
            packetService.RegisterCommand(new StartCommand());
            packetService.RegisterCommand(new QuitCommand());

            await packetService.SendCommandAsync<HelloCommand>(playerName);

            return new BattleGame(tcp, connection, packetService, playerName, isHost: false);
        }

        /// <summary>
        /// <para>
        /// Host on a given port. Will return once a client has connected.
        /// </para>
        /// <para>
        /// On connection error, use <see cref="SocketException.ErrorCode"/> from the thrown error to obtain the cause of the error.
        /// Refer to the <see href="https://docs.microsoft.com/en-us/windows/desktop/winsock/windows-sockets-error-codes-2">Windows Sockets version 2 API error code</see> documentation.
        /// </para>
        /// </summary>
        /// <param name="port">Host port.</param>
        /// <param name="playerName">Name of the hosting player.</param>
        /// <exception cref="SocketException">An error occurred when accessing the socket.</exception>
        [NotNull]
        public static async Task<BattleGame> HostAndWaitAsync(ushort port, string playerName)
        {
            TcpListener listener = TcpListener.Create(port);
            try
            {
                listener.Start();

                TcpClient tcp = await listener.AcceptTcpClientAsync();
                var connection = new StreamConnection(tcp.GetStream());
                var packetService = new PacketService(connection);

                await connection.SendResponseAsync(new Response
                {
                    Code = ResponseCode.VersionGreeting,
                    Message = ProtocolVersion
                });
                
                packetService.RegisterCommand(new FireCommand());
                packetService.RegisterCommand(new HelloCommand());
                packetService.RegisterCommand(new HelpCommand());
                packetService.RegisterCommand(new StartCommand());
                packetService.RegisterCommand(new QuitCommand());

                return new BattleGame(tcp, connection, packetService, playerName, true);
            }
            finally
            {
                listener.Stop();
            }
        }

        public static string HelloWorld()
        {
            return "Hello World!";
        }

        public virtual void Dispose()
        {
            _client.Dispose();
            _connection.Dispose();
            PacketService.Dispose();
        }
    }
}