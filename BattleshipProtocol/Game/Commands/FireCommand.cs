using System.Collections.Generic;
using BattleshipProtocol.Protocol;

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

        /// <inheritdoc />
        public void OnCommand(PacketConnection context, string argument)
        {
            // TODO: Validate game state
            // TODO: Fire on our grid
            // TODO: Send response of result
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public void OnResponse(PacketConnection context, Response response)
        {
            // TODO: Validate game state
            // TODO: Register fire on their grid
            throw new System.NotImplementedException();
        }
    }
}