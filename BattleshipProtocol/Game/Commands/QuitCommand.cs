using System;
using System.Threading.Tasks;
using BattleshipProtocol.Protocol;
using BattleshipProtocol.Protocol.Exceptions;

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

        /// <inheritdoc />
        public async Task OnCommandAsync(PacketConnection context, string argument)
        {
            await context.SendResponseAsync(ResponseCode.ConnectionClosed, "Connection closed");
            context.Dispose();
        }

        /// <inheritdoc />
        public Task OnResponseAsync(PacketConnection context, Response response)
        {
            context.Dispose();
            return Task.CompletedTask;
        }
    }
}