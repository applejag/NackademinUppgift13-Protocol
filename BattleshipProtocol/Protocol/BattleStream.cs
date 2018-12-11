using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BattleshipProtocol.Protocol.Exceptions;
using BattleshipProtocol.Protocol.Internal;
using BattleshipProtocol.Protocol.Internal.Extensions;
using JetBrains.Annotations;

namespace BattleshipProtocol.Protocol
{
    public class BattleStream : IDisposable, IObservable<IPacket>
    {
        private readonly Stream _stream;
        private readonly StreamReader _reader;
        private readonly StreamWriter _writer;

        private readonly SemaphoreSlim _writerSemaphore = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _readerSemaphore = new SemaphoreSlim(1, 1);

        private readonly Regex _commandRegex =
            new Regex(@"^([a-z]+)(?: (.*))?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly Regex _responseRegex =
            new Regex(@"^([0-9]{1,3})(?: (.*))?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly HashSet<ICommandTemplate> _registeredCommands = new HashSet<ICommandTemplate>();

        private readonly HashSet<IObserver<IPacket>> _packetObservers = new HashSet<IObserver<IPacket>>();

        public bool ConnectionOpen { get; private set; } = true;

        [NotNull, ItemNotNull]
        public IReadOnlyCollection<ICommandTemplate> RegisteredCommands => _registeredCommands;

        public int ReadTimeout
        {
            get => _stream.ReadTimeout;
            set => _stream.ReadTimeout = value;
        }

        /// <inheritdoc />
        /// <summary>
        /// Initializes the Battleship commands stream with encoding <see cref="P:System.Text.Encoding.UTF8" />
        /// </summary>
        /// <param name="stream">The stream to use when reading and writing data.</param>
        public BattleStream([NotNull] in Stream stream)
            : this(stream, Encoding.UTF8)
        {
        }

        /// <inheritdoc />
        /// <summary>
        /// Initializes the Battleship commands stream with custom encoding.
        /// </summary>
        /// <param name="stream">The stream to use when reading and writing data.</param>
        /// <param name="encoding">The encoding to use when reading and writing data.</param>
        public BattleStream([NotNull] in Stream stream, [NotNull] in Encoding encoding)
        {
            _stream = stream;

            _reader = new StreamReader(stream, encoding, 
                detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);

            _writer = new StreamWriter(stream, encoding, 
                bufferSize: 1024, leaveOpen: true);
        }

        #region Readers and writers of protocol types

        [Pure]
        private bool TryParseResponse([NotNull] in string source, out Response response)
        {
            response = default;

            // Check if response code
            Match match = _responseRegex.Match(source);

            if (!match.Success)
                return false;

            if (!Enum.TryParse(match.Groups[1].Value, out ResponseCode code))
                return false;

            // Validate
            if (!Enum.IsDefined(typeof(ResponseCode), code))
            {
                throw new ProtocolUnknownResponseException((short)code);
            }

            // Register response received
            response = new Response
            {
                Source = source,
                Code = code,
                Message = match.Groups[2].Value
            };

            return true;
        }

        [Pure]
        private bool TryParseCommand([NotNull] in string source, out ReceivedCommand command)
        {
            command = default;

            // Check if command
            Match match = _commandRegex.Match(source);

            if (!match.Success)
                return false;

            string commandCode = match.Groups[1].Value;
            ICommandTemplate commandTemplate = GetCommand(commandCode);

            // Validate
            if (commandTemplate is null)
            {
                throw new ProtocolUnknownCommandException(commandCode);
            }

            // Register received command
            string argument = match.Groups[2].Value;

            command = new ReceivedCommand
            {
                Source = source,
                CommandTemplate = commandTemplate,
                Argument = argument
            };

            return true;
        }

        /// <summary>
        /// Starts the reading loop.
        /// To catch these messages, subscribe using the <see cref="Subscribe"/> method.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if it's already listening.</exception>
        public async void BeginListening()
        {
            if (_readerSemaphore.CurrentCount == 0)
                throw new InvalidOperationException("This stream is already listening.");

            using (await _readerSemaphore.EnterAsync())
            {
                while (ConnectionOpen)
                {
                    await ReceiveAsyncInternal();
                }
            }
        }

        /// <summary>
        /// Waits and receives one response or command.
        /// To catch these messages, subscribe using the <see cref="Subscribe"/> method.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the connection has been closed.</exception>
        [NotNull]
        private async Task ReceiveAsyncInternal()
        {
            if (!ConnectionOpen)
                throw new InvalidOperationException("Stream has closed!");

            try
            {
                string line = await ReadLineAsyncInternal();

                if (line is null)
                    return;

                if (TryParseResponse(in line, out Response response))
                {
                    OnResponseReceived(in response);
                    return;
                }

                if (TryParseCommand(in line, out ReceivedCommand command))
                {
                    OnCommandReceived(in command);
                    return;
                }

                // "WHAT YOU MEAN??"
                throw new ProtocolFormatException(line);
            }
            catch (ProtocolException error)
            {
                await SendErrorAsync(error);
                OnError(error);
            }
            catch (Exception unexpected)
            {
                OnError(in unexpected);
            }
        }

        [NotNull]
        public Task<Response> ExpectResponseAsync(in int timeoutOverride)
        {
            int oldTimeout = _stream.ReadTimeout;
            _stream.ReadTimeout = timeoutOverride;

            try
            {
                return ExpectResponseAsync();
            }
            finally
            {
                _stream.ReadTimeout = oldTimeout;
            }
        }

        [NotNull]
        public async Task<Response> ExpectResponseAsync()
        {
            if (!ConnectionOpen)
                throw new InvalidOperationException("Stream has closed!");

            if (_readerSemaphore.CurrentCount == 0)
                throw new InvalidOperationException("Stream has closed!");
            
            using (await _readerSemaphore.EnterAsync())
            {
                string line = await ReadLineAsyncInternal();

                if (line is null)
                    throw new ProtocolUnexpectedDisconnect();

                if (!TryParseResponse(line, out Response response))
                    throw new ProtocolFormatException(line);

                OnResponseReceived(response);
                return response;
            }
        }

        [NotNull]
        public Task<ReceivedCommand> ExpectCommandAsync(in int timeoutOverride)
        {
            int oldTimeout = _stream.ReadTimeout;
            _stream.ReadTimeout = timeoutOverride;

            try
            {
                return ExpectCommandAsync();
            }
            finally
            {
                _stream.ReadTimeout = oldTimeout;
            }
        }

        [NotNull]
        public async Task<ReceivedCommand> ExpectCommandAsync()
        {
            if (!ConnectionOpen)
                throw new InvalidOperationException("Stream has closed!");

            if (_readerSemaphore.CurrentCount == 0)
                throw new InvalidOperationException("Stream has closed!");

            using (await _readerSemaphore.EnterAsync())
            {
                string line = await ReadLineAsyncInternal();

                if (line is null)
                    throw new ProtocolUnexpectedDisconnect();

                if (!TryParseCommand(line, out ReceivedCommand command))
                    throw new ProtocolFormatException(line);

                OnCommandReceived(command);
                return command;
            }
        }

        [NotNull, ItemCanBeNull]
        private async Task<string> ReadLineAsyncInternal()
        {
            string line;

            try
            {
                line = await _reader.ReadLineAsync();
            }
            catch
            {
                line = null;
            }

            if (line is null)
                Dispose();

            return line;
        }

        /// <summary>
        /// Send a textblock (asynchronously) to the other client.
        /// </summary>
        /// <param name="text">The text to transmit.</param>
        /// <exception cref="InvalidOperationException">Thrown if the connection has been closed.</exception>
        public async Task SendTextAsync(string text)
        {
            if (!ConnectionOpen)
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
            if (!ConnectionOpen)
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
            if (!ConnectionOpen)
                throw new InvalidOperationException("Stream has closed!");

            using (await _writerSemaphore.EnterAsync())
            {
                await _writer.WriteLineAsync(response.ToString());
                await _writer.FlushAsync();
            }
        }

        /// <summary>
        /// Send a command (asynchronously) to the other client, with the optional argument.
        /// </summary>
        /// <param name="argument">The optional argument to append to the command. Should not contain newlines.</param>
        /// <exception cref="ArgumentException">Thrown if <typeparamref name="T"/> did not match any registered command.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the connection has been closed.</exception>
        [NotNull]
        public async Task SendCommandAsync<T>([CanBeNull] string argument = null) where T : class, ICommandTemplate
        {
            if (!ConnectionOpen)
                throw new InvalidOperationException("Stream has closed!");

            var commandTemplate = GetCommand<T>();

            if (commandTemplate is null)
                throw new ArgumentException($"Command {typeof(T).Name} is not registered for this stream.", nameof(T));

            await SendCommandAsyncInternal(commandTemplate, argument);
        }

        private async Task SendCommandAsyncInternal([NotNull] ICommandTemplate commandTemplate, [CanBeNull] string argument)
        {
            using (await _writerSemaphore.EnterAsync())
            {
                await _writer.WriteLineAsync(string.IsNullOrEmpty(argument)
                    ? commandTemplate.Command
                    : $"{commandTemplate.Command} {argument}");

                await _writer.FlushAsync();
            }
        }

        /// <summary>
        /// Gets first command by name via <see cref="ICommandTemplate.Command"/> (case insensitive). Returns null if no match.
        /// </summary>
        /// <param name="command">The command name to match.</param>
        [Pure, CanBeNull]
        public ICommandTemplate GetCommand([NotNull] string command)
        {
            return _registeredCommands.FirstOrDefault(cmd =>
                cmd.Command.Equals(command, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Gets first command by type. Returns null if no match.
        /// </summary>
        /// <typeparam name="T">The command type to match.</typeparam>
        [Pure, CanBeNull]
        public T GetCommand<T>() where T : ICommandTemplate
        {
            return (T)_registeredCommands.FirstOrDefault(cmd => cmd is T);
        }

        /// <summary>
        /// Register a command template to listen for. Commands must be unique to their <see cref="ICommandTemplate.Command"/> property (case insensitive).
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="commandTemplate"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <see cref="ICommandTemplate.Command">Command</see> property of <paramref name="commandTemplate"/> is already registered.</exception>
        /// <param name="commandTemplate">The command template.</param>
        public void RegisterCommand([NotNull] ICommandTemplate commandTemplate)
        {
            if (commandTemplate is null)
                throw new ArgumentNullException(nameof(commandTemplate));

            if (_registeredCommands.Contains(commandTemplate))
                throw new ArgumentException(
                    $"Command {commandTemplate.Command.ToUpper()} is already registered.",
                    nameof(commandTemplate));

            _registeredCommands.Add(commandTemplate);
        }

        #endregion

        public void Dispose()
        {
            if (!ConnectionOpen) return;
            
            ConnectionOpen = false;
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
            foreach (IObserver<IPacket> observer in _packetObservers)
            {
                observer.OnError(error);
            }
        }

        protected virtual void OnCommandReceived(in ReceivedCommand packet)
        {
            foreach (IObserver<IPacket> observer in _packetObservers)
            {
                observer.OnNext(packet);
            }
        }

        protected virtual void OnResponseReceived(in Response packet)
        {
            foreach (IObserver<IPacket> observer in _packetObservers)
            {
                observer.OnNext(packet);
            }
        }

        protected virtual void OnStreamClosed()
        {
            foreach (IObserver<IPacket> observer in _packetObservers)
            {
                observer.OnCompleted();
            }
        }

        public IDisposable Subscribe(IObserver<IPacket> observer)
        {
            if (observer is null)
                throw new ArgumentNullException(nameof(observer));

            if (!_packetObservers.Contains(observer))
                _packetObservers.Add(observer);

            return new ObserverUnsubscriber<IPacket>(_packetObservers, observer);
        }
    }
}