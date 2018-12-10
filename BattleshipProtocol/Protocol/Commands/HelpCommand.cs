using System;

namespace BattleshipProtocol.Protocol.Commands
{
    public class HelpCommand : ICommandTemplate
    {
        /// <inheritdoc />
        public string Command { get; } = "HELP";

        /// <inheritdoc />
        public void OnCommand(BattleGame context, string argument)
        {
            // TODO: Display help
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public void OnResponse(BattleGame context, Response response)
        {
            throw new NotSupportedException();
        }
    }
}