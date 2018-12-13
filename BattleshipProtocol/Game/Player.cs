using System.Net;

namespace BattleshipProtocol.Game
{
    public class Player
    {
        public Board Board { get; set; }
        public string Name { get; set; }
        public bool IsLocal { get; }
        public EndPoint EndPoint { get; }

        public Player(bool isLocal, EndPoint endPoint)
        {
            IsLocal = isLocal;
            EndPoint = endPoint;
        }
    }
}