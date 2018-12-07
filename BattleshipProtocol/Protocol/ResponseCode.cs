using BattleshipProtocol.Game;

namespace BattleshipProtocol.Protocol
{
    public enum ResponseCode : short
    {
        /// <summary>Client bound. Greeting. Sent by server on connection established.</summary>
        VersionGreeting = 210,

        /// <summary>Server bound. Handshake response, includes other players name.</summary>
        Handshake = 220,

        /// <summary>Client bound. Sent by server upon new game, to indicate the client starts.</summary>
        StartClient = 221,
        /// <summary>Client bound. Sent by server upon new game, to indicate the server starts.</summary>
        StartHost = 222,

        /// <summary>Client/server bound. Sent as response to a FIRE. Indicates a miss.</summary>
        FireMiss = 230,

        /// <summary>Client/server bound. Sent as response to a FIRE. Indicates a hit on the <see cref="ShipType.Carrier"/> ship.</summary>
        FireHitCarrier = 241,
        /// <summary>Client/server bound. Sent as response to a FIRE. Indicates a hit on the <see cref="ShipType.Battleship"/> ship.</summary>
        FireHitBattleship = 242,
        /// <summary>Client/server bound. Sent as response to a FIRE. Indicates a hit on the <see cref="ShipType.Destroyer"/> ship.</summary>
        FireHitDestroyer = 243,
        /// <summary>Client/server bound. Sent as response to a FIRE. Indicates a hit on the <see cref="ShipType.Submarine"/> ship.</summary>
        FireHitSubmarine = 244,
        /// <summary>Client/server bound. Sent as response to a FIRE. Indicates a hit on the <see cref="ShipType.PatrolBoat"/> ship.</summary>
        FireHitPatrolBoat = 245,

        /// <summary>Client/server bound. Sent as response to a FIRE. Indicates that the <see cref="ShipType.Carrier"/> ship has been sunk. This is sent instead of <see cref="FireHitCarrier"/></summary>
        FireSunkCarrier = 251,
        /// <summary>Client/server bound. Sent as response to a FIRE. Indicates that the <see cref="ShipType.Battleship"/> ship has been sunk. This is sent instead of <see cref="FireHitBattleship"/></summary>
        FireSunkBattleship = 252,
        /// <summary>Client/server bound. Sent as response to a FIRE. Indicates that the <see cref="ShipType.Destroyer"/> ship has been sunk. This is sent instead of <see cref="FireHitDestroyer"/></summary>
        FireSunkDestroyer = 253,
        /// <summary>Client/server bound. Sent as response to a FIRE. Indicates that the <see cref="ShipType.Submarine"/> ship has been sunk. This is sent instead of <see cref="FireHitSubmarine"/></summary>
        FireSunkSubmarine = 254,
        /// <summary>Client/server bound. Sent as response to a FIRE. Indicates that the <see cref="ShipType.PatrolBoat"/> ship has been sunk. This is sent instead of <see cref="FireHitPatrolBoat"/></summary>
        FireSunkPatrolBoat = 255,

        /// <summary>Client/server bound. Sent as response to a FIRE. Indicates that the receiver of the response won the game (sunk the last ship). This is sent instead of any <see cref="FireHitBattleship"/> or <see cref="FireSunkBattleship"/> response.</summary>
        FireYouWin = 260,

        /// <summary>Client bound. The connection has been closed by the server.</summary>
        ConnectionClosed = 270,

        /// <summary>
        /// Client/server bound.
        /// Sent as response when the opponent has sent a command or response that failed to parse.
        /// Example: "2b1" will fail to parse as neither command nor response.
        /// </summary>
        SyntaxError = 500,

        /// <summary>
        /// Client/server bound.
        /// Sent as response when the opponent has sent a command or response, during an invalid state of the game (for that message).
        /// Example: a FIRE command when it's not their turn.
        /// </summary>
        SequenceError = 501,
    }
}