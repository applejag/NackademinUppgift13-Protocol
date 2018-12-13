using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BattleshipProtocol.Protocol.Exceptions;
using BattleshipProtocol.Protocol.Internal.Extensions;
using JetBrains.Annotations;

namespace BattleshipProtocol.Protocol.Internal
{
    public class StreamConnection : IDisposable, IObservable<string>
    {
        private readonly Stream _stream;
        private readonly StreamReader _reader;
        private readonly StreamWriter _writer;

        private readonly SemaphoreSlim _writerSemaphore = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _readerSemaphore = new SemaphoreSlim(1, 1);
        private readonly HashSet<IObserver<string>> _observers = new HashSet<IObserver<string>>();

        private CancellationTokenSource _listenCancellationTokenSource;

        public bool IsConnected { get; private set; } = true;
        public bool IsListening => _listenCancellationTokenSource != null;

        public int ReadTimeout {
            get => _stream.ReadTimeout;
            set => _stream.ReadTimeout = value;
        }

        /// <inheritdoc />
        /// <summary>
        /// Initializes the stream connection with <see cref="Encoding.UTF8"/> encoding.
        /// </summary>
        /// <param name="stream">The stream to use when reading and writing data.</param>
        public StreamConnection([NotNull] in Stream stream)
            : this(stream, Encoding.UTF8)
        {
        }

        /// <summary>
        /// Initializes the stream connection with custom encoding.
        /// </summary>
        /// <param name="stream">The stream to use when reading and writing data.</param>
        /// <param name="encoding">The encoding to use when reading and writing data.</param>
        public StreamConnection([NotNull] in Stream stream, [NotNull] in Encoding encoding)
        {
            _stream = stream;

            _reader = new StreamReader(stream, encoding,
                detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);

            _writer = new StreamWriter(stream, encoding,
                bufferSize: 1024, leaveOpen: true);
        }

        #region Readers and writers of protocol types

        /// <summary>
        /// Starts the reading loop.
        /// To catch these messages, subscribe using the <see cref="Subscribe"/> method.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if it's already listening.</exception>
        public void BeginListening()
        {
            if (!_readerSemaphore.Wait(0))
                throw new InvalidOperationException("This stream is already listening.");

            _listenCancellationTokenSource = new CancellationTokenSource();

            Task.Run(() =>
            {
                while (IsConnected)
                {
                    ReadAndHandleInternal();
                }
            }, _listenCancellationTokenSource.Token).ContinueWith(t =>
            {
                if (!t.IsCanceled && t.IsFaulted)
                    OnError(t.Exception);
                
                _readerSemaphore.Release();
            });
        }

        /// <summary>
        /// Waits for one line of text.
        /// To catch these messages, subscribe using the <see cref="Subscribe"/> pattern.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the connection has been closed.</exception>
        private void ReadAndHandleInternal()
        {
            if (!IsConnected)
                throw new InvalidOperationException("Stream has closed!");

            string line;

            try
            {
                line = _reader.ReadLine();
            }
            catch (Exception unexpected)
            {
                OnError(in unexpected);
                line = null;
            }

            if (line is null)
                Dispose();
            else
                OnStringLineReceived(line);
        }

        /// <summary>
        /// Waits and returns one line of text. This bypasses the <see cref="IObservable{T}.Subscribe"/> pattern.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the connection has been closed.</exception>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="BeginListening"/> is running.</exception>
        [CanBeNull]
        public string ReadLine()
        {
            if (!IsConnected)
                throw new InvalidOperationException("Stream has closed!");

            if (IsListening)
                throw new InvalidOperationException("Stream is already being read by " + nameof(BeginListening) + ".");

            return _reader.ReadLine();
        }

        /// <summary>
        /// Waits and returns one line of text. This bypasses the <see cref="IObservable{T}.Subscribe"/> pattern.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the connection has been closed.</exception>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="BeginListening"/> is running.</exception>
        [NotNull, ItemCanBeNull]
        public async Task<string> ReadLineAsync()
        {
            if (!IsConnected)
                throw new InvalidOperationException("Stream has closed!");

            if (IsListening)
                throw new InvalidOperationException("Stream is already being read by " + nameof(BeginListening) + ".");

            return await _reader.ReadLineAsync();
        }

        /// <summary>
        /// Send a textblock (asynchronously) to the other client.
        /// </summary>
        /// <param name="text">The text to transmit.</param>
        /// <exception cref="InvalidOperationException">Thrown if the connection has been closed.</exception>
        [NotNull]
        public async Task SendTextAsync(string text)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Stream has closed!");

            using (await _writerSemaphore.EnterAsync())
            {
                foreach (string line in text.Split(new[] { "\n\r", "\r\n", "\r", "\n" }, StringSplitOptions.None))
                {
                    await _writer.WriteLineAsync(line);
                }
                await _writer.FlushAsync();
            }
        }

        /// <summary>
        /// Send an exception (asynchronously) to the other client in the form of a response code.
        /// </summary>
        /// <param name="error">The exception to transmit.</param>
        /// <exception cref="InvalidOperationException">Thrown if the connection has been closed.</exception>
        public async Task SendErrorAsync(ProtocolException error)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Stream has closed!");

            using (await _writerSemaphore.EnterAsync())
            {
                await _writer.WriteLineAsync($"{(short)error.ErrorCode} {error.ErrorMessage}");
                await _writer.FlushAsync();
            }
        }

        /// <summary>
        /// Send a response (asynchronously) to the other client.
        /// </summary>
        /// <param name="response">The response to transmit.</param>
        /// <exception cref="InvalidOperationException">Thrown if the connection has been closed.</exception>
        public async Task SendResponseAsync(Response response)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Stream has closed!");

            using (await _writerSemaphore.EnterAsync())
            {
                await _writer.WriteLineAsync(response.ToString());
                await _writer.FlushAsync();
            }
        }

        /// <summary>
        /// Send a response (asynchronously) to the other client.
        /// </summary>
        /// <param name="code">The response code to transmit.</param>
        /// <param name="message">The optional message to append to the response.</param>
        /// <exception cref="InvalidOperationException">Thrown if the connection has been closed.</exception>
        public Task SendResponseAsync(ResponseCode code, [CanBeNull] string message)
        {
            return SendResponseAsync(new Response(code, message));
        }

        /// <summary>
        /// Send a command (asynchronously) to the other client.
        /// </summary>
        /// <param name="commandTemplate">The command to transmit.</param>
        /// <param name="argument">The optional argument.</param>
        /// <exception cref="InvalidOperationException">Thrown if the connection has been closed.</exception>
        protected async Task SendCommandAsync([NotNull] ICommandTemplate commandTemplate, [CanBeNull] string argument)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Stream has closed!");

            using (await _writerSemaphore.EnterAsync())
            {
                await _writer.WriteLineAsync(string.IsNullOrEmpty(argument)
                    ? commandTemplate.Command
                    : $"{commandTemplate.Command} {argument}");

                await _writer.FlushAsync();
            }
        }

        #endregion

        public virtual void Dispose()
        {
            if (!IsConnected) return;

            if (IsListening)
                _listenCancellationTokenSource?.Cancel();

            IsConnected = false;
            OnStreamClosed();

            _reader.Dispose();
            _stream.Dispose();
            try
            {
                _writer.Dispose();
            }
            catch
            {
                // StreamWriter is a beach
            }
        }

        protected virtual void OnError(in Exception error)
        {
            foreach (IObserver<string> observer in _observers.ToList())
            {
                observer.OnError(error);
            }
        }

        protected virtual void OnStringLineReceived(in string packet)
        {
            foreach (IObserver<string> observer in _observers.ToList())
            {
                observer.OnNext(packet);
            }
        }

        protected virtual void OnStreamClosed()
        {
            foreach (IObserver<string> observer in _observers.ToList())
            {
                observer.OnCompleted();
            }
        }

        IDisposable IObservable<string>.Subscribe(IObserver<string> observer)
        {
            if (observer is null)
                throw new ArgumentNullException(nameof(observer));

            if (!_observers.Contains(observer))
                _observers.Add(observer);

            return new UnsubscribingObserver<string>(_observers, observer);
        }
    }
}