using System;
using BattleshipProtocol.Protocol;

namespace BattleshipProtocol.Game.Commands
{
    public class HelpCommand : ICommandTemplate
    {
        /// <inheritdoc />
        public string Command { get; } = "HELP";

        /// <inheritdoc />
        public ResponseCode[] RoutedResponseCodes { get; } = { };

        /// <inheritdoc />
        public void OnCommand(in PacketConnection context, in string argument)
        {
            // TODO: Display help
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public void OnResponse(in PacketConnection context, in Response response)
        {
            throw new NotSupportedException();
        }
    }
}