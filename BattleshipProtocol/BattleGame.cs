using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using BattleshipProtocol.Game.Commands;
using BattleshipProtocol.Protocol;
using BattleshipProtocol.Protocol.Exceptions;
using BattleshipProtocol.Protocol.Internal.Extensions;
using JetBrains.Annotations;

namespace BattleshipProtocol
{
    public class BattleGame : IDisposable
    {
        public const string ProtocolVersion = "BATTLESHIP/1.0";

        private readonly TcpClient _client;
        private readonly BattleStream _stream;
        public bool IsHost { get; }
        public string PlayerName { get; }
        public string OpponentName { get; private set; }

        public IObservable<IPacket> PacketProvider => _stream;

        public bool IsConnected => _stream.ConnectionOpen;
        public EndPoint RemoteEndPoint => _client.Client.RemoteEndPoint;

        private BattleGame(TcpClient client, BattleStream stream, string playerName, bool isHost)
        {
            IsHost = isHost;
            _client = client;
            _stream = stream;

            _stream.BeginListening();
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
        /// <exception cref="SocketException">An error occurred when accessing the socket.</exception>
        /// <exception cref="ArgumentNullException">The <paramref name="address"/> is not null.</exception>
        /// <exception cref="ProtocolException">Version handshake failed.</exception>
        [NotNull]
        public static async Task<BattleGame> ConnectAsync([NotNull] string address, ushort port, [NotNull] string playerName)
        {
            var tcp = new TcpClient();

            await tcp.ConnectAsync(address, port);
            var stream = new BattleStream(tcp.GetStream());

            await stream.EnsureVersionGreeting(ProtocolVersion);

            stream.RegisterCommand(new FireCommand());
            stream.RegisterCommand(new HelloCommand());
            stream.RegisterCommand(new HelpCommand());
            stream.RegisterCommand(new StartCommand());
            stream.RegisterCommand(new QuitCommand());

            await stream.SendCommandAsync<HelloCommand>(playerName);

            return new BattleGame(tcp, stream, playerName, isHost: false);
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
        /// <exception cref="SocketException">An error occurred when accessing the socket.</exception>
        [NotNull]
        public static async Task<BattleGame> HostAndWaitAsync(ushort port, string playerName)
        {
            TcpListener listener = TcpListener.Create(port);
            try
            {
                listener.Start();

                TcpClient tcp = await listener.AcceptTcpClientAsync();
                var stream = new BattleStream(tcp.GetStream());

                await stream.SendResponseAsync(new Response
                {
                    Code = ResponseCode.VersionGreeting,
                    Message = ProtocolVersion
                });
                
                stream.RegisterCommand(new FireCommand());
                stream.RegisterCommand(new HelloCommand());
                stream.RegisterCommand(new HelpCommand());
                stream.RegisterCommand(new StartCommand());
                stream.RegisterCommand(new QuitCommand());

                return new BattleGame(tcp, stream, playerName, true);
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
            _stream.Dispose();
        }
    }
}