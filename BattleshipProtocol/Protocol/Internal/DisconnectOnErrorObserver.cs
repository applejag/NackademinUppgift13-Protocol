using System;

namespace BattleshipProtocol.Protocol.Internal
{
    public class DisconnectOnErrorObserver : IObserver<IPacket>
    {
        public int ConsecutiveErrorCount { get; set; }
        public int ConsecutiveErrorLimit { get; set; }
        private readonly BattleGame _game;

        private DisconnectOnErrorObserver(BattleGame game)
        {
            _game = game;
        }

        public static IDisposable SubscribeTo(BattleGame game)
        {
            var observer = new DisconnectOnErrorObserver(game);
            return game.PacketConnection.Subscribe(observer);
        }

        public void OnNext(IPacket value)
        {
            ConsecutiveErrorCount = 0;
        }

        public void OnError(Exception error)
        {
            ConsecutiveErrorCount++;

            if (ConsecutiveErrorCount > ConsecutiveErrorLimit)
            {
                _game.Dispose();
            }
        }

        public void OnCompleted()
        {}
    }
}