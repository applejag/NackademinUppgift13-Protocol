using System;
using BattleshipProtocol.Protocol;

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
        public async void OnCommand(PacketConnection context, string argument)
        {
            await context.SendResponseAsync(new Response
            {
                Code = ResponseCode.ConnectionClosed,
                Message = "Connection closed"
            });
            context.Dispose();
        }

        /// <inheritdoc />
        public void OnResponse(PacketConnection context, Response response)
        {
            context.Dispose();
        }
    }
}