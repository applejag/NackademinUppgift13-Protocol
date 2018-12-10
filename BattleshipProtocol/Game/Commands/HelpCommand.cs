using System;
using BattleshipProtocol.Protocol;

namespace BattleshipProtocol.Game.Commands
{
    public class HelpCommand : ICommandTemplate
    {
        /// <inheritdoc />
        public string Command { get; } = "HELP";

        /// <inheritdoc />
        public void OnCommand(BattleClient context, string argument)
        {
            // TODO: Display help
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public void OnResponse(BattleClient context, Response response)
        {
            throw new NotSupportedException();
        }
    }
}