using System.Threading.Tasks;
using BattleshipProtocol.Protocol;
using BattleshipProtocol.Protocol.Internal.Extensions;

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
        private readonly BattleGame _game;

        public StartCommand(BattleGame game)
        {
            _game = game;
        }

        /// <inheritdoc />
        public Task OnCommandAsync(PacketConnection context, string argument)
        {
            _game.ThrowIfNotHost(Command);
            _game.ThrowIfWrongState(Command, GameState.Idle);

            _game.GameState = GameState.InGame;
            // TODO: Randomize player turn
            // TODO: Send turn to remote
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public Task OnResponseAsync(PacketConnection context, Response response)
        {
            _game.ThrowIfHost(response.Code);
            _game.ThrowIfWrongState(response.Code, GameState.Idle);

            _game.GameState = GameState.InGame;
            // TODO: Set player turn
            throw new System.NotImplementedException();
        }
    }
}