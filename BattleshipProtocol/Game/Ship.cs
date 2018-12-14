using System;
using System.ComponentModel;
using JetBrains.Annotations;

namespace BattleshipProtocol.Game
{
    public class Ship
    {
        /// <summary>
        /// Gets the ship name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the ship type.
        /// </summary>
        public ShipType Type { get; }

        /// <summary>
        /// Gets the length for bounding box of this ship.
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// Gets the length of bounding box in the south direction.
        /// </summary>
        public int LengthSouth => Orientation == Orientation.South ? Length : 1;

        /// <summary>
        /// Gets the length of bounding box in the east direction.
        /// </summary>
        public int LengthEast => Orientation == Orientation.East ? Length : 1;

        /// <summary>
        /// Gets the remaining health of this ship.
        /// </summary>
        public int Health { get; internal set; }

        /// <summary>
        /// Gets the orientation of the boat. Facing.
        /// </summary>
        public Orientation Orientation { get; private set; }

        /// <summary>
        /// Gets the X position of this ship, relative to the north-west corner of the ship. 0 is far west (A). 9 is far east (J).
        /// Is set to -1 if unknown or unset location (for example, pre-placed and the opponents ships).
        /// </summary>
        public int X { get; private set; } = -1;

        /// <summary>
        /// Gets the Y position of this ship, relative to the north-west corner of the ship. 0 is far north (1). 9 is far south (10).
        /// Is set to -1 if unknown or unset location (for example, pre-placed and the opponents ships).
        /// </summary>
        public int Y { get; private set; } = -1;

        /// <summary>
        /// Gets whether this boat on the grid? I.e. has it been placed by the user. 
        /// </summary>
        public bool IsOnBoard => X != -1;

        public Ship(in ShipType type)
        {
            Name = GetShipName(in type);
            Type = type;
            Length = GetShipLength(in type);
            Health = Length;
            Orientation = Orientation.South;
        }

        /// <summary>
        /// Sets the position values of this ship. Throws error if outside the grid. Does not check for collision with other boats.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="coordinate"/>+<see cref="Length"/> is beyond the east map boundary when <paramref name="orientation"/> is <see cref="Game.Orientation.South">South</see>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="coordinate"/>+<see cref="Length"/> is beyond the south map boundary when <paramref name="orientation"/> is <see cref="Game.Orientation.East">East</see>.</exception>
        /// <param name="coordinate">The position to check.</param>
        /// <param name="orientation">The orientation. Facing north or facing east.</param>
        internal void SetPositionInternal(in Coordinate coordinate, in Orientation orientation)
        {
            if (coordinate.X > 10 - LengthEast)
                throw new ArgumentOutOfRangeException(nameof(coordinate), "Ship is lapping outside the map on the east edge due to length and orientation.");

            if (coordinate.Y > 10 - LengthSouth)
                throw new ArgumentOutOfRangeException(nameof(coordinate), "Ship is lapping outside the map on the south edge due to length and orientation.");

            X = coordinate.X;
            Y = coordinate.Y;
            Orientation = orientation;
        }

        /// <summary>
        /// Would this ship collide with a ship that would be placed at this location, with this orientation, and with that length?
        /// </summary>
        /// <param name="other">Position of theoretical other ship.</param>
        /// <param name="otherOrientation">The orientation of theoretical other ship. Facing north or facing east.</param>
        /// <param name="otherLength">Length of theoretical other ship.</param>
        [Pure]
        public bool WillCollide(in Coordinate other, in Orientation otherOrientation, in int otherLength)
        {
            if (!IsOnBoard)
                return false;

            if (otherOrientation == Orientation)
            {
                switch (otherOrientation)
                {
                    case Orientation.East:
                        return other.Y == Y
                               && !(X >= other.X + otherLength || other.X >= X + Length);

                    case Orientation.South:
                        return other.X == X
                               && !(Y >= other.Y + otherLength || other.Y >= Y + Length);

                    default:
                        return false;
                }
            }

            switch (otherOrientation)
            {
                case Orientation.South:
                    return !(Y < other.Y || Y >= other.Y + otherLength
                                   || X > other.X || other.X >= X + Length);

                case Orientation.East:
                    return !(Y > other.Y || other.Y >= Y + Length
                                   || X < other.X || X >= other.X + otherLength);

                default:
                    return false;
            }
        }

        /// <summary>
        /// Is this coordinate on this ship?
        /// </summary>
        /// <param name="coordinate">Position to check.</param>
        [Pure]
        public bool IsOnShip(in Coordinate coordinate)
        {
            switch (Orientation)
            {
                case Orientation.East when coordinate.X >= X && coordinate.X < X + Length && coordinate.Y == Y:
                case Orientation.South when coordinate.X == X && coordinate.Y >= Y && coordinate.Y < Y + Length:
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
        public static int GetShipLength(in ShipType type)
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

        private static string GetShipName(in ShipType shipType)
        {
            switch (shipType)
            {
                case ShipType.Carrier:
                    return "Carrier";
                case ShipType.Battleship:
                    return "Battleship";
                case ShipType.Destroyer:
                    return "Destroyer";
                case ShipType.Submarine:
                    return "Submarine";
                case ShipType.PatrolBoat:
                    return "Patrol boat";
                default:
                    throw new ArgumentOutOfRangeException(nameof(shipType), shipType, null);
            }
        }
    }
}