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
        private readonly Ship[,] _shipsHit = new Ship[10, 10];

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
        /// <exception cref="InvalidOperationException">If the shot ship is registering an invalid location. See <see cref="TryCalculateShipPosition"/>.</exception>
        /// <param name="coordinate">Position to check.</param>
        /// <param name="hitShip">The ship that was hit, or null for miss.</param>
        [CanBeNull]
        internal Ship RegisterShot(in Coordinate coordinate, in ShipType? hitShip)
        {
            if (IsShotAt(in coordinate))
                throw new InvalidOperationException($"Board has already been shot at {coordinate}");

            Ship ship = hitShip.HasValue ? GetShip(hitShip.Value) : null;

            if (ship != null && !ship.IsOnBoard)
            {
                List<Coordinate> shotsOnShip = GetShotsOnShip(ship.Type)
                    .Append(coordinate).ToList();
                if (TryCalculateShipPosition(in ship, shotsOnShip, out Coordinate shipCoordinate,
                    out Orientation shipOrientation))
                {
                    if (ship.Health == 1)
                        MoveShip(ship.Type, shipCoordinate, shipOrientation);
                }
            }

            _shots[coordinate.X, coordinate.Y] = true;
            _shipsHit[coordinate.X, coordinate.Y] = ship;

            OnBoardShot(in coordinate);

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
        /// Returns a list of the coordinates where this ship has been shot.
        /// </summary>
        /// <param name="shipType">The ship to compare.</param>
        [NotNull, Pure]
        public List<Coordinate> GetShotsOnShip(in ShipType shipType)
        {
            var coordinates = new List<Coordinate>();

            for (var x = 0; x < 10; x++)
            {
                for (var y = 0; y < 10; y++)
                {
                    if (_shipsHit[x, y]?.Type != shipType) continue;

                    coordinates.Add((x,y));

                    if (coordinates.Count == _shipsHit[x, y].Length)
                        return coordinates;
                }
            }

            return coordinates;
        }

        /// <summary>
        /// Validates a remote ship and where it has been shot in an effort to calculate its location.
        /// </summary>
        /// <param name="ship">The ship to validate.</param>
        /// <param name="coordinates">The coordinates of where this ship has been shot, assuming sorted 0-9 on x and y.</param>
        /// <param name="coordinate">The coordinate of this ship, if found.</param>
        /// <param name="orientation">The orientation of this ship, if found.</param>
        /// <exception cref="InvalidOperationException">The shot coordinates are more than the health of the ship.</exception>
        /// <exception cref="InvalidOperationException">The shot coordinates do not fall on a straight line.</exception>
        /// <exception cref="InvalidOperationException">The shot coordinates grasp a grander boundary than possible for the ship.</exception>
        [Pure]
        public bool TryCalculateShipPosition([NotNull] in Ship ship, [NotNull] List<Coordinate> coordinates, out Coordinate coordinate, out Orientation orientation)
        {
            // Location already found
            if (ship.IsOnBoard)
            {
                coordinate = (ship.X, ship.Y);
                orientation = ship.Orientation;
                return false;
            }

            // No shots
            if (coordinates.Count == 0)
            {
                coordinate = default;
                orientation = default;
                return false;
            }

            if (coordinates.Count > ship.Length)
                throw new InvalidOperationException($"There has occurred more shots on the {ship.Name} than it has max health.");

            // Determine orientation
            Coordinate first = coordinates[0];
            var coordinates1d = new int[coordinates.Count];

            if (TestDimension(0))
            {
                orientation = Orientation.East;
            }
            else if (TestDimension(1))
            {
                orientation = Orientation.South;
            }
            else
            {
                throw new InvalidOperationException("The shot locations does not lie in a straight line.");
            }

            // Check range
            int min = coordinates1d.Min();
            int max = coordinates1d.Max();
            int range = max - min;
            if (range > ship.Length)
            {
                throw new InvalidOperationException($"The shot locations spans a longer distance than the max distance for a {ship.Name}.");
            }

            // Undeterminable
            if (range != ship.Length)
            {
                coordinate = default;
                return false;
            }

            // Gottem, assuming they're sorted.
            coordinate = first;
            return true;

            bool TestDimension(int dimension)
            {
                int firstValue = first[dimension];
                coordinates1d[0] = firstValue;

                for (var i = 0; i < coordinates.Count; i++)
                {
                    if (coordinates[i][dimension] != firstValue)
                        return false;

                    coordinates1d[i] = coordinates[i][dimension];
                }

                return true;
            }
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
        /// If a ship of unknown location (ex: remote board) was hit at <paramref name="coordinate"/> then this returns that ship.
        /// </summary>
        /// <param name="coordinate">Position to check.</param>
        [CanBeNull, Pure]
        public Ship GetShipAt(in Coordinate coordinate)
        {
            if (_shipsHit[coordinate.X, coordinate.Y] is Ship s)
                return s;

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
                if (other == ship)
                    continue;
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