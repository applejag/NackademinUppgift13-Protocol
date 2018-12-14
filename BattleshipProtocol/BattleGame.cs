using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using BattleshipProtocol.Game;
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

        /// <summary>
        /// Gets whether this application is the server.
        /// </summary>
        public bool IsHost { get; }

        /// <summary>
        /// Gets the local player object. That is- information about the player in this application.
        /// </summary>
        [NotNull]
        public Player LocalPlayer { get; }

        /// <summary>
        /// Gets the remote player object. That is- information about the other player.
        /// </summary>
        [NotNull]
        public Player RemotePlayer { get; }

        /// <summary>
        /// Gets or sets the state of the connection and game.
        /// </summary>
        public GameState GameState { get; set; }

        /// <summary>
        /// Gets or sets whether it's the local players turn. If not, it's the remote players turn.
        /// </summary>
        public bool IsLocalsTurn { get; set; }

        public PacketConnection PacketConnection { get; }

        private BattleGame(TcpClient client, PacketConnection packetConnection, Board localBoard, string playerName, bool isHost)
        {
            IsHost = isHost;
            RemotePlayer = new Player(false, client.Client.RemoteEndPoint);
            LocalPlayer = new Player(true, client.Client.LocalEndPoint)
                {Name = playerName, Board = localBoard};

            _client = client;
            PacketConnection = packetConnection;

            packetConnection.RegisterCommand(new FireCommand(this));
            packetConnection.RegisterCommand(new HelloCommand(this));
            packetConnection.RegisterCommand(new HelpCommand());
            packetConnection.RegisterCommand(new StartCommand(this));
            packetConnection.RegisterCommand(new QuitCommand(this));

            ForwardErrorsObserver.SubscribeTo(this);
            PacketConnection.Subscribe(new PacketObserver(this));

            PacketConnection.BeginListening();
            GameState = GameState.Handshake;
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
        /// <param name="localBoard">The board of the local player.</param>
        /// <param name="localPlayerName">The name of the local player.</param>
        /// <param name="timeout">Timeout in milliseconds for awaiting version handshake.</param>
        /// <exception cref="SocketException">An error occurred when accessing the socket.</exception>
        /// <exception cref="ArgumentNullException">The <paramref name="address"/> parameter is null.</exception>
        /// <exception cref="ProtocolException">Version handshake failed.</exception>
        /// <exception cref="SocketException">An error occurred when accessing the socket.</exception>
        /// <exception cref="ArgumentNullException">The <paramref name="localPlayerName"/> parameter is null or whitespace.</exception>
        /// <exception cref="ArgumentException">The ships in the <paramref name="localBoard"/> parameter is not set up.</exception>
        [NotNull]
        public static async Task<BattleGame> ConnectAsync([NotNull] string address, ushort port, 
            [NotNull] Board localBoard, [NotNull] string localPlayerName, int timeout = 10_000)
        {
            if (string.IsNullOrWhiteSpace(localPlayerName))
                throw new ArgumentNullException(nameof(localPlayerName), "Name of local player must be set.");

            if (!localBoard.Ships.All(s => s.IsOnBoard))
                throw new ArgumentException("Local board is not set up!", nameof(localBoard));

            var tcp = new TcpClient();

            await tcp.ConnectAsync(address, port);
            var connection = new PacketConnection(tcp.GetStream());

            await connection.EnsureVersionGreeting(ProtocolVersion, timeout);

            var game = new BattleGame(tcp, connection, localBoard, localPlayerName, isHost: false);

            await game.PacketConnection.SendCommandAsync<HelloCommand>(localPlayerName);

            return game;
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
        /// <param name="localBoard">The board of the local player.</param>
        /// <param name="localPlayerName">The name of the local player.</param>
        /// <exception cref="SocketException">An error occurred when accessing the socket.</exception>
        /// <exception cref="ArgumentNullException">The <paramref name="localPlayerName"/> parameter is null or whitespace.</exception>
        /// <exception cref="ArgumentException">The ships in the <paramref name="localBoard"/> parameter is not set up.</exception>
        [NotNull]
        public static async Task<BattleGame> HostAndWaitAsync(ushort port,
            [NotNull] Board localBoard, [NotNull] string localPlayerName)
        {
            if (string.IsNullOrWhiteSpace(localPlayerName))
                throw new ArgumentNullException(nameof(localPlayerName), "Name of local player must be set.");

            if (!localBoard.Ships.All(s => s.IsOnBoard))
                throw new ArgumentException("Local board is not set up!", nameof(localBoard));

            TcpListener listener = TcpListener.Create(port);
            try
            {
                listener.Start();

                TcpClient tcp = await listener.AcceptTcpClientAsync();
                var connection = new PacketConnection(tcp.GetStream());

                await connection.SendResponseAsync(new Response
                {
                    Code = ResponseCode.VersionGreeting,
                    Message = ProtocolVersion
                });

                return new BattleGame(tcp, connection, localBoard, localPlayerName, true);
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
        }

        private class PacketObserver : IObserver<IPacket>
        {
            private readonly BattleGame _game;

            public PacketObserver(BattleGame game)
            {
                _game = game;
            }

            public void OnNext(IPacket value)
            {
            }

            public void OnError(Exception error)
            {
            }

            public void OnCompleted()
            {
                _game.GameState = GameState.Disconnected;
            }
        }
    }
}