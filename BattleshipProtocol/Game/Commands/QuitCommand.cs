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
        public void OnCommand(PacketConnection context, string argument)
        {
            // TODO: Close connection
            // TODO: Send 270
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public void OnResponse(PacketConnection context, Response response)
        {
            // TODO: Is 270? Then close connection
            throw new NotSupportedException();
        }
    }
}