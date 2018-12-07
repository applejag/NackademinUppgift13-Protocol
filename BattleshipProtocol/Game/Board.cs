using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using JetBrains.Annotations;

namespace BattleshipProtocol.Game
{
    /// <summary>
    /// A 10 by 10 game board. Stores info about shot history and ships. Checks for ship placement collisions.
    /// </summary>
    public class Board
    {
        protected internal bool[,] _shots = new bool[10, 10];

        [NotNull, ItemNotNull]
        public IReadOnlyCollection<Ship> Ships { get; }

        public Board()
        {
            Ships = Array.ConvertAll((ShipType[]) Enum.GetValues(typeof(ShipType)), s => new Ship(s));
        }

        /// <summary>
        /// Get the ship of a certain type from this board.
        /// </summary>
        /// <param name="type">The ship type.</param>
        [NotNull, Pure]
        public Ship GetShip(ShipType type)
        {
            foreach (Ship ship in Ships)
            {
                if (ship.Type == type)
                    return ship;
            }

            throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(ShipType));
        }

        /// <summary>
        /// Shoot at a grid location.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">If position from <paramref name="x"/> and <paramref name="y"/> is outside the grid.</exception>
        /// <param name="x">X position to check. 0 is far west (A). 9 is far east (J).</param>
        /// <param name="y">Y position to check. 0 is far north (1). 9 is far south (10).</param>
        internal void ShootAtInternal(int x, int y)
        {
            if (x < 0 || x > 9)
                throw new ArgumentOutOfRangeException(nameof(x), "Valid position ranges from 0-9.");

            if (y < 0 || y > 9)
                throw new ArgumentOutOfRangeException(nameof(y), "Valid position ranges from 0-9.");

            if (IsShotAt(x, y))
                throw new InvalidOperationException("Board has already been shot at that location.");

            // TODO: Logic for sending shot command
        }

        /// <summary>
        /// Get the ship at position.
        /// </summary>
        /// <param name="x">X position to check. 0 is far west (A). 9 is far east (J).</param>
        /// <param name="y">Y position to check. 0 is far north (1). 9 is far south (10).</param>
        [CanBeNull, Pure]
        public Ship GetShipAt(int x, int y)
        {
            if (x < 0 || x > 9)
                throw new ArgumentOutOfRangeException(nameof(x), "Valid position ranges from 0-9.");

            if (y < 0 || y > 9)
                throw new ArgumentOutOfRangeException(nameof(y), "Valid position ranges from 0-9.");

            return Ships.FirstOrDefault(ship => ship.IsOnShip(x, y));
        }

        /// <summary>
        /// Has this position been shot at?
        /// </summary>
        /// <param name="x">X position to check. 0 is far west (A). 9 is far east (J).</param>
        /// <param name="y">Y position to check. 0 is far north (1). 9 is far south (10).</param>
        [Pure]
        public bool IsShotAt(int x, int y)
        {
            if (x < 0 || x > 9)
                throw new ArgumentOutOfRangeException(nameof(x), "Valid position ranges from 0-9.");

            if (y < 0 || y > 9)
                throw new ArgumentOutOfRangeException(nameof(y), "Valid position ranges from 0-9.");

            return _shots[x, y];
        }

        /// <summary>
        /// Moves a ship to a new location on this grid. Throws error if placed outside grid or collides with other ship.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="x"/> is outside the map.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="x"/>+<see cref="Ship.Length"/> is outside the map and iff <paramref name="orientation"/> is <see cref="Game.Orientation.South">South</see>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="y"/> is outside the map.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="y"/>+<see cref="Ship.Length"/> is outside the map and iff <paramref name="orientation"/> is <see cref="Game.Orientation.East">East</see>.</exception>
        /// <exception cref="InvalidOperationException">If ship collides with other ship.</exception>
        /// <param name="shipType">The type of the ship to move.</param>
        /// <param name="x">X position to check. 0 is far west (A). 9 is far east (J).</param>
        /// <param name="y">Y position to check. 0 is far north (1). 9 is far south (10).</param>
        /// <param name="orientation">The orientation. Facing north or facing east.</param>
        public void MoveShip(ShipType shipType, int x, int y, Orientation orientation)
        {
            Ship ship = GetShip(shipType);

            if (x < 0 || x > 9)
                throw new ArgumentOutOfRangeException(nameof(x), "Valid position ranges from 0-9.");

            if (y < 0 || y > 9)
                throw new ArgumentOutOfRangeException(nameof(y), "Valid position ranges from 0-9.");

            if (x > 10 - ship.LengthEast)
                throw new ArgumentOutOfRangeException(nameof(x), "Ship is lapping outside the map on the east edge due to length and orientation.");

            if (y > 10 - ship.LengthSouth)
                throw new ArgumentOutOfRangeException(nameof(x), "Ship is lapping outside the map on the south edge due to length and orientation.");

            if (Ships.Any(other => other.WillCollide(x, y, orientation, ship.Length)))
                throw new InvalidOperationException("Cannot move ship to that location due to collision with other ship!");

            ship.SetPositionInternal(x, y, orientation);
        }

        /// <summary>
        /// Is the position inside the game board?
        /// </summary>
        /// <param name="x">X position to check. 0 is far west (A). 9 is far east (J).</param>
        /// <param name="y">Y position to check. 0 is far north (1). 9 is far south (10).</param>
        [Pure]
        public static bool IsOnBoard(int x, int y)
        {
            return x >= 0 && x <= 9
                && y >= 0 && y <= 9;
        }
    }
}