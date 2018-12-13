using System.Threading.Tasks;
using BattleshipProtocol.Protocol;

namespace BattleshipProtocol.Game.Commands
{
    public class StartCommand : ICommandTemplate
    {
        /// <inheritdoc />
        public string Command { get; } = "START";

        /// <inheritdoc />
        public ResponseCode[] RoutedResponseCodes { get; } =
        {
            ResponseCode.StartClient,
            ResponseCode.StartHost
        };

        /// <inheritdoc />
        public Task OnCommandAsync(PacketConnection context, string argument)
        {
            // TODO: Validate game state
            // TODO: Switch to game-phase
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public Task OnResponseAsync(PacketConnection context, Response response)
        {
            // TODO: Validate game state
            // TODO: Switch to game-phase
            throw new System.NotImplementedException();
        }
    }
}