using System;
using System.Collections.Generic;

namespace BattleshipProtocol.Protocol.Internal
{
    internal class UnsubscribingObserver<T> : IDisposable
    {
        private readonly ICollection<IObserver<T>> _observers;
        private readonly IObserver<T> _observer;

        public UnsubscribingObserver(ICollection<IObserver<T>> observers, IObserver<T> observer)
        {
            this._observers = observers;
            this._observer = observer;
        }

        public void Dispose()
        {
            if (!(_observer is null)) _observers.Remove(_observer);
        }
    }
}