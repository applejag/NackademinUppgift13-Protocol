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
        public async Task OnCommandAsync(PacketConnection context, string argument)
        {
            _game.ThrowIfNotHost(Command);

            await context.SendResponseAsync(ResponseCode.ConnectionClosed, "Connection closed");
            context.Dispose();
        }

        /// <inheritdoc />
        public Task OnResponseAsync(PacketConnection context, Response response)
        {
            _game.ThrowIfHost(response.Code);

            context.Dispose();
            return Task.CompletedTask;
        }
    }
}