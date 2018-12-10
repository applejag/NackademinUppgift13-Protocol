using BattleshipProtocol.Protocol;

namespace BattleshipProtocol.Game.Commands
{
    public class StartCommand : ICommandTemplate
    {
        /// <inheritdoc />
        public string Command { get; } = "START";

        /// <inheritdoc />
        public void OnCommand(BattleGame context, string argument)
        {
            // TODO: Validate game state
            // TODO: Switch to game-phase
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public void OnResponse(BattleGame context, Response response)
        {
            // TODO: Validate game state
            // TODO: Switch to game-phase
            throw new System.NotImplementedException();
        }
    }
}