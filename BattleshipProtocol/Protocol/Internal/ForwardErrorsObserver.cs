using System;
using BattleshipProtocol.Protocol.Exceptions;

namespace BattleshipProtocol.Protocol.Internal
{
    public class ForwardErrorsObserver : IObserver<IPacket>
    {
        private readonly BattleGame _game;

        private ForwardErrorsObserver(BattleGame game)
        {
            _game = game;
        }

        public static IDisposable SubscribeTo(BattleGame game)
        {
            var observer = new ForwardErrorsObserver(game);
            return game.PacketConnection.Subscribe(observer);
        }

        public void OnNext(IPacket value)
        { }

        public async void OnError(Exception exception)
        {
            
            if (exception is ProtocolException error)
            {
                await _game.PacketConnection.SendErrorAsync(error);
            }
            else
            {
                await _game.PacketConnection.SendErrorAsync(new ProtocolException(ResponseCode.SyntaxError,
                    "Unexpected exception: " + exception.Message));
            }
        }

        public void OnCompleted()
        { }
    }
}