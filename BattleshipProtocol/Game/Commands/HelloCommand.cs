using BattleshipProtocol.Protocol;

namespace BattleshipProtocol.Game.Commands
{
    public class HelloCommand : ICommandTemplate
    {
        /// <inheritdoc />
        public string Command { get; } = "HELO";

        /// <inheritdoc />
        public void OnCommand(BattleGame context, string argument)
        {
            // TODO: Save name (from message) into opponent game object and send a 220 response with our name
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public void OnResponse(BattleGame context, Response response)
        {
            // TODO: Save name (from response) into opponent game object
            throw new System.NotImplementedException();
        }
    }
}