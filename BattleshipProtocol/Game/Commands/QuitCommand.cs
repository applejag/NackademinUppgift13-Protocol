using System;
using System.Threading.Tasks;
using BattleshipProtocol.Protocol;
using BattleshipProtocol.Protocol.Exceptions;
using BattleshipProtocol.Protocol.Internal.Extensions;

namespace BattleshipProtocol.Game.Commands
{
    public class QuitCommand : ICommandTemplate
    {
        /// <inheritdoc />
        public string Command { get; } = "QUIT";

        /// <inheritdoc />
        public ResponseCode[] RoutedResponseCodes { get; } =
        {
            ResponseCode.ConnectionClosed
        };

        private readonly BattleGame _game;

        public QuitCommand(BattleGame game)
        {
            _game = game;
        }

        /// <inheritdoc />
        public Task OnCommandAsync(PacketConnection context, string argument)
        {
            _game.ThrowIfNotHost(Command);

            _game.GameState = GameState.Disconnected;
            return _game.Disconnect();
        }

        /// <inheritdoc />
        public Task OnResponseAsync(PacketConnection context, Response response)
        {
            _game.ThrowIfHost(response.Code);

            _game.GameState = GameState.Disconnected;
            context.Dispose();
            return Task.CompletedTask;
        }
    }
}