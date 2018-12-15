namespace BattleshipProtocol.Game
{
    public struct FireOutcome
    {
        /// <summary>
        /// The coordinate of the action.
        /// </summary>
        public Coordinate Coordinate { get; set; }

        /// <summary>
        /// The ship that got hit, or null on miss.
        /// </summary>
        public Ship ShipHit { get; set; }

        /// <summary>
        /// Did the hit ship get sunk?
        /// </summary>
        public bool ShipSunk { get; set; }

        public FireOutcome(Coordinate coordinate, Ship shipHit)
        {
            Coordinate = coordinate;
            ShipHit = shipHit;
            ShipSunk = shipHit?.Health == 0;
        }
    }
}