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
        private readonly bool[,] _shots = new bool[10, 10];

        public event EventHandler<Coordinate> BoardShot;

        /// <summary>
        /// The collection of ships in this board. Is guaranteed to be only one of each <see cref="ShipType"/>.
        /// </summary>
        [NotNull, ItemNotNull]
        public IReadOnlyCollection<Ship> Ships { get; }

        public Board()
        {
            Ships = Array.ConvertAll((ShipType[])Enum.GetValues(typeof(ShipType)), s => new Ship(s));
        }

        /// <summary>
        /// Get the ship of a certain type from this board.
        /// </summary>
        /// <param name="type">The ship type.</param>
        [NotNull, Pure]
        public Ship GetShip(in ShipType type)
        {
            foreach (Ship ship in Ships)
            {
                if (ship.Type == type)
                    return ship;
            }

            throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(ShipType));
        }

        /// <summary>
        /// Register a shot and decrement the ships health. Returns the ship that was shot, if any.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">If position from <paramref name="coordinate"/> is outside the grid.</exception>
        /// <exception cref="InvalidOperationException">If board has already been shot at the given <paramref name="coordinate"/></exception>
        /// <param name="coordinate">Position to check.</param>
        /// <param name="hitShip">The ship that was hit, or null for miss.</param>
        [CanBeNull]
        internal Ship RegisterShot(in Coordinate coordinate, in ShipType? hitShip)
        {
            if (IsShotAt(in coordinate))
                throw new InvalidOperationException($"Board has already been shot at {coordinate}");

            _shots[coordinate.X, coordinate.Y] = true;
            OnBoardShot(in coordinate);

            Ship ship = hitShip.HasValue ? GetShip(hitShip.Value) : null;

            if (ship is null)
                return null;

            if (ship.Health == 0)
            {
                throw new InvalidOperationException($"Ship has already been sunk.");
            }

            ship.Health--;
            return ship;
        }

        /// <summary>
        /// Shoot at a grid location. Returns the ship that was shot, if any.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">If position from <paramref name="coordinate"/> is outside the grid.</exception>
        /// <exception cref="InvalidOperationException">If board has already been shot at the given <paramref name="coordinate"/></exception>
        /// <param name="coordinate">Position to check.</param>
        [CanBeNull]
        internal Ship ShootAtInternal(in Coordinate coordinate)
        {
            Ship ship = GetShipAt(in coordinate);
            RegisterShot(coordinate, ship?.Type);

            return ship;
        }

        /// <summary>
        /// Get the ship at position.
        /// </summary>
        /// <param name="coordinate">Position to check.</param>
        [CanBeNull, Pure]
        public Ship GetShipAt(in Coordinate coordinate)
        {
            foreach (Ship ship in Ships)
            {
                if (ship.IsOnShip(in coordinate))
                    return ship;
            }

            return null;
        }

        /// <summary>
        /// Has this position been shot at?
        /// </summary>
        /// <param name="coordinate">Position to check.</param>
        [Pure]
        public bool IsShotAt(in Coordinate coordinate)
        {
            return _shots[coordinate.X, coordinate.Y];
        }

        /// <summary>
        /// Moves a ship to a new location on this grid. Throws error if placed outside grid or collides with other ship.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="coordinate"/>+<see cref="Ship.LengthEast"/> is outside the map.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="coordinate"/>+<see cref="Ship.LengthSouth"/> is outside the map.</exception>
        /// <exception cref="InvalidOperationException">If ship collides with other ship.</exception>
        /// <param name="shipType">The type of the ship to move.</param>
        /// <param name="coordinate">Position to check.</param>
        /// <param name="orientation">The orientation. Facing north or facing east.</param>
        public void MoveShip(ShipType shipType, in Coordinate coordinate, in Orientation orientation)
        {
            Ship ship = GetShip(in shipType);

            foreach (Ship other in Ships)
            {
                if (other.WillCollide(in coordinate, in orientation, ship.Length))
                    throw new InvalidOperationException("Cannot move ship to that location due to collision with other ship!");
            }

            ship.SetPositionInternal(in coordinate, in orientation);
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

        protected virtual void OnBoardShot(in Coordinate e)
        {
            BoardShot?.Invoke(this, e);
        }
    }
}