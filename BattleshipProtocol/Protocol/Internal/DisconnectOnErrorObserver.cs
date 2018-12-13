using System;
using BattleshipProtocol.Protocol.Exceptions;

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

        public static IDisposable SubscribeTo(BattleGame game, int errorLimit = 3)
        {
            var observer = new DisconnectOnErrorObserver(game)
            {
                ConsecutiveErrorLimit = errorLimit
            };
            return game.PacketConnection.Subscribe(observer);
        }

        public void OnNext(IPacket value)
        {
            ConsecutiveErrorCount = 0;
        }

        public async void OnError(Exception error)
        {
            ConsecutiveErrorCount++;

            if (ConsecutiveErrorCount > ConsecutiveErrorLimit)
            {
                await _game.PacketConnection.SendErrorAsync(new ProtocolTooManyErrorsException(ConsecutiveErrorCount));
                _game.Dispose();
            }
        }

        public void OnCompleted()
        {}
    }
}