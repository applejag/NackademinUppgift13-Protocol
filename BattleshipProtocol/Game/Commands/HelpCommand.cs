using System;
using System.Threading.Tasks;
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
        public void OnCommand(PacketConnection context, string argument)
        {

        }

        /// <inheritdoc />
        public void OnResponse(PacketConnection context, Response response)
        {
            throw new NotSupportedException();
        }
    }
}