using System.Collections.Generic;
using System.Threading.Tasks;
using BattleshipProtocol.Protocol;
using BattleshipProtocol.Protocol.Internal.Extensions;

namespace BattleshipProtocol.Game.Commands
{
    public class FireCommand : ICommandTemplate
    {
        /// <inheritdoc />
        public string Command { get; } = "FIRE";

        public ResponseCode[] RoutedResponseCodes { get; } =
        {
            ResponseCode.FireMiss,

            ResponseCode.FireHitCarrier,
            ResponseCode.FireHitBattleship,
            ResponseCode.FireHitDestroyer,
            ResponseCode.FireHitSubmarine,
            ResponseCode.FireHitPatrolBoat,

            ResponseCode.FireSunkCarrier,
            ResponseCode.FireSunkBattleship,
            ResponseCode.FireSunkDestroyer,
            ResponseCode.FireSunkSubmarine,
            ResponseCode.FireSunkPatrolBoat,

            ResponseCode.FireYouWin,
        };

        private readonly BattleGame _game;

        public FireCommand(BattleGame game)
        {
            _game = game;
        }

        /// <inheritdoc />
        public Task OnCommandAsync(PacketConnection context, string argument)
        {
            _game.ThrowIfWrongState(Command, GameState.InGame);
            // TODO: Validate game state
            // TODO: Fire on our grid
            // TODO: Send response of result
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public Task OnResponseAsync(PacketConnection context, Response response)
        {
            _game.ThrowIfWrongState(response.Code, GameState.InGame);
            // TODO: Validate game state
            // TODO: Register fire on their grid
            throw new System.NotImplementedException();
        }
    }
}