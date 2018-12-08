using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BattleshipProtocol.Protocol.Extensions
{
    internal static class ThreadingExtensions
    {
        /// <summary>
        /// Wait to enter the semaphore, then returns a disposable object that releases from the semaphore when disposed.
        /// Meant to be used in a using() {} block.
        /// </summary>
        /// <param name="semaphore">The semaphore.</param>
        [NotNull, MustUseReturnValue]
        public static async Task<IDisposable> EnterAsync([NotNull] this SemaphoreSlim semaphore)
        {
            await semaphore.WaitAsync();
            return new DisposableSemaphoreEnter(semaphore);
        }

        private class DisposableSemaphoreEnter : IDisposable
        {
            private readonly SemaphoreSlim _semaphore;

            public DisposableSemaphoreEnter(SemaphoreSlim semaphore)
            {
                _semaphore = semaphore;
            }

            public void Dispose()
            {
                _semaphore.Release();
            }
        }
    }
}
