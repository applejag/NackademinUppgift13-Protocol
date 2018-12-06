using System;
using System.ComponentModel;
using JetBrains.Annotations;

namespace BattleshipProtocol.Game
{
    public class Ship
    {
        /// <summary>
        /// Ship type.
        /// </summary>
        public ShipType Type { get; }

        /// <summary>
        /// Length for bounding box of this ship.
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// Length of bounding box in the south direction.
        /// </summary>
        public int LengthSouth => Orientation == Orientation.South ? Length : 1;

        /// <summary>
        /// Length of bounding box in the east direction.
        /// </summary>
        public int LengthEast => Orientation == Orientation.East ? Length : 1;

        /// <summary>
        /// Remaining health of this ship.
        /// </summary>
        public int Health { get; }

        /// <summary>
        /// The orientation of the boat. Facing.
        /// </summary>
        public Orientation Orientation { get; private set; }

        /// <summary>
        /// X position of this ship, relative to the north-west corner of the ship. 0 is far west (A). 9 is far east (J).
        /// Set to -1 if unknown or unset location (for example, pre-placed and the opponents ships).
        /// </summary>
        public int X { get; private set; } = -1;

        /// <summary>
        /// Y position of this ship, relative to the north-west corner of the ship. 0 is far north (1). 9 is far south (10).
        /// Set to -1 if unknown or unset location (for example, pre-placed and the opponents ships).
        /// </summary>
        public int Y { get; private set; } = -1;

        /// <summary>
        /// Is this boat on the grid? I.e. has it been placed by the user. 
        /// </summary>
        public bool IsOnBoard => X != -1;

        public Ship(ShipType type)
        {
            Type = type;
            Length = GetShipLength(type);
            Health = Length;
            Orientation = Orientation.South;
        }

        /// <summary>
        /// Sets the position values of this ship. Throws error if outside the grid. Does not check for collision with other boats.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="x"/> is outside the map.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="x"/>+<see cref="Length"/> is outside the map and iff <paramref name="orientation"/> is <see cref="Game.Orientation.South">South</see>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="y"/> is outside the map.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="y"/>+<see cref="Length"/> is outside the map and iff <paramref name="orientation"/> is <see cref="Game.Orientation.East">East</see>.</exception>
        /// <param name="x">X position to check. 0 is far west (A). 9 is far east (J).</param>
        /// <param name="y">Y position to check. 0 is far north (1). 9 is far south (10).</param>
        /// <param name="orientation">The orientation. Facing north or facing east.</param>
        internal void SetPositionInternal(int x, int y, Orientation orientation)
        {
            if (x < 0 || x > 9)
                throw new ArgumentOutOfRangeException(nameof(x), "Valid position ranges from 0-9.");

            if (y < 0 || y > 9)
                throw new ArgumentOutOfRangeException(nameof(y), "Valid position ranges from 0-9.");

            if (x > 10 - LengthEast)
                throw new ArgumentOutOfRangeException(nameof(x), "Ship is lapping outside the map on the east edge due to length and orientation.");

            if (y > 10 - LengthSouth)
                throw new ArgumentOutOfRangeException(nameof(x), "Ship is lapping outside the map on the south edge due to length and orientation.");

            X = x;
            Y = y;
            Orientation = orientation;
        }

        /// <summary>
        /// Would this ship collide with a ship that would be placed at this location, with this orientation, and with that length?
        /// </summary>
        /// <param name="otherX">X position of theoretical other ship. 0 is far west (A). 9 is far east (J).</param>
        /// <param name="otherY">Y position of theoretical other ship. 0 is far north (1). 9 is far south (10).</param>
        /// <param name="otherOrientation">The orientation of theoretical other ship. Facing north or facing east.</param>
        /// <param name="otherLength">Length of theoretical other ship.</param>
        [Pure]
        public bool WillCollide(int otherX, int otherY, Orientation otherOrientation, int otherLength)
        {
            if (!IsOnBoard)
                return false;

            if (otherOrientation == Orientation)
            {
                switch (otherOrientation)
                {
                    case Orientation.East:
                        return otherY == Y
                               && !(X >= otherX + otherLength || otherX >= X + Length);

                    case Orientation.South:
                        return otherX == X
                               && !(Y >= otherY + otherLength || otherY >= Y + Length);

                    default:
                        return false;
                }
            }

            switch (otherOrientation)
            {
                case Orientation.South:
                    return !(Y < otherY || Y >= otherY + otherLength
                                   || X > otherX || otherX >= X + Length);

                case Orientation.East:
                    return !(Y > otherY || otherY >= Y + Length
                                   || X < otherX || X >= otherX + otherLength);

                default:
                    return false;
            }
        }

        /// <summary>
        /// Is this coordinate on this ship?
        /// </summary>
        /// <param name="x">X position to check. 0 is far west (A). 9 is far east (J).</param>
        /// <param name="y">Y position to check. 0 is far north (1). 9 is far south (10).</param>
        public bool IsOnShip(int x, int y)
        {
            switch (Orientation)
            {
                case Orientation.East when x >= X && x < X + Length && y == Y:
                case Orientation.South when x == X && y >= Y && y < Y + Length:
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Get the length of a certain ship type.
        /// </summary>
        /// <exception cref="InvalidEnumArgumentException"></exception>
        /// <param name="type">The ship type.</param>
        [Pure]
        public static int GetShipLength(ShipType type)
        {
            switch (type)
            {
                case ShipType.Carrier:
                    return 5;

                case ShipType.Battleship:
                    return 4;

                case ShipType.Destroyer:
                    return 3;

                case ShipType.Submarine:
                    return 3;

                case ShipType.PatrolBoat:
                    return 2;
            }

            throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(ShipType));
        }
    }
}