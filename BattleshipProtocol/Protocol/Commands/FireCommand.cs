namespace BattleshipProtocol.Protocol.Commands
{
    public class FireCommand : ICommand
    {
        /// <inheritdoc />
        public string Command { get; } = "START";

        /// <inheritdoc />
        public void OnCommand(BattleGame context, string argument)
        {
            // TODO: Validate game state
            // TODO: Fire on our grid
            // TODO: Send response of result
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public void OnResponse(BattleGame context, Response response)
        {
            // TODO: Validate game state
            // TODO: Register fire on their grid
            throw new System.NotImplementedException();
        }
    }
}