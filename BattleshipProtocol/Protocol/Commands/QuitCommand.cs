using System;

namespace BattleshipProtocol.Protocol.Commands
{
    public class QuitCommand : ICommandFactory
    {
        /// <inheritdoc />
        public string Command { get; } = "QUIT";

        /// <inheritdoc />
        public void OnCommand(BattleGame context, string argument)
        {
            // TODO: Close connection
            // TODO: Send 270
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public void OnResponse(BattleGame context, Response response)
        {
            // TODO: Is 270? Then close connection
            throw new NotSupportedException();
        }
    }
}