using System.Net;
using JetBrains.Annotations;

namespace BattleshipProtocol.Game
{
    public class Player
    {
        [NotNull]
        public Board Board { get; set; }

        [CanBeNull]
        public string Name { get; set; }

        public bool IsLocal { get; }

        [NotNull]
        public EndPoint EndPoint { get; }

        public Player(bool isLocal, EndPoint endPoint)
        {
            IsLocal = isLocal;
            EndPoint = endPoint;
            Board = new Board();
        }
    }
}