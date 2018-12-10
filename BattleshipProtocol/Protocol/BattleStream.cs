using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BattleshipProtocol.Protocol.Commands;
using BattleshipProtocol.Protocol.Extensions;
using BattleshipProtocol.Protocol.Internal;
using JetBrains.Annotations;

namespace BattleshipProtocol.Protocol
{
    public class BattleStream : IDisposable, IObservable<IPacket>
    {
        private readonly Stream _stream;
        private readonly StreamReader _reader;
        private readonly StreamWriter _writer;

        private readonly SemaphoreSlim _writerSemaphore = new SemaphoreSlim(1, 1);

        private readonly Regex _commandRegex =
            new Regex(@"^([a-z]+)(?: (.*))?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly Regex _responseRegex =
            new Regex(@"^([0-9]{1,3})(?: (.*))?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public bool ConnectionOpen { get; private set; }

        [NotNull, ItemNotNull]
        private readonly HashSet<ICommandTemplate> _registeredCommands = new HashSet<ICommandTemplate>();

        [NotNull, ItemNotNull]
        private readonly HashSet<IObserver<IPacket>> _packetObservers = new HashSet<IObserver<IPacket>>();

        [NotNull, ItemNotNull]
        public IReadOnlyCollection<ICommandTemplate> RegisteredCommands => _registeredCommands;

        public event EventHandler StreamClosed;

        /// <inheritdoc />
        /// <summary>
        /// Initializes the Battleship commands stream with encoding <see cref="P:System.Text.Encoding.UTF8" />
        /// </summary>
        /// <param name="stream">The stream to use when reading and writing data.</param>
        public BattleStream([NotNull] Stream stream)
            : this(stream, Encoding.UTF8)
        {
        }

        /// <inheritdoc />
        /// <summary>
        /// Initializes the Battleship commands stream with custom encoding.
        /// </summary>
        /// <param name="stream">The stream to use when reading and writing data.</param>
        /// <param name="encoding">The encoding to use when reading and writing data.</param>
        public BattleStream([NotNull] Stream stream, [NotNull] Encoding encoding)
        {
            _stream = stream;
            _reader = new StreamReader(stream, encoding);
            _writer = new StreamWriter(stream, encoding);
        }

        #region Readers and writers of protocol types

        private async Task<bool> TryHandleResponseAsync(string source)
        {
            // Check if response code
            Match match = _responseRegex.Match(source);

            if (!match.Success)
                return false;

            var code = (ResponseCode) short.Parse(match.Groups[1].Value);

            // Validate
            if (!Enum.IsDefined(typeof(ResponseCode), code))
            {
                await SendAsync(new Response
                {
                    Code = ResponseCode.SyntaxError,
                    Message = $"Syntax error: Unknown response code {code}"
                });

                return false;
            }

            // Register response received
            OnResponseReceived(new Response
            {
                Source = source,
                Code = code,
                Message = match.Groups[2].Value
            });
            return true;
        }

        private async Task<bool> TryHandleCommandAsync(string source)
        {
            // Check if command
            Match match = _commandRegex.Match(source);

            if (!match.Success)
                return false;

            string commandCode = match.Groups[1].Value;
            ICommandTemplate commandTemplate = GetCommand(commandCode);

            // Validate
            if (commandTemplate is null)
            {
                await SendAsync(new Response
                {
                    Code = ResponseCode.SyntaxError,
                    Message = $"Syntax error: Command \"{commandCode.ToUpperInvariant()}\" not found"
                });
                return false;
            }

            // Register received command
            string argument = match.Groups[2].Value;

            OnCommandReceived(new ReceivedCommand
            {
                Source = source,
                CommandTemplate = commandTemplate,
                Argument = argument
            });
            return true;
        }

        /// <summary>
        /// Waits and receives one response or command.
        /// To catch these messages, see <see cref="CommandReceived">CommandReceived</see> and <see cref="ResponseReceived">ResponseReceived</see>
        /// </summary>
        [NotNull]
        public async Task ReceiveAsync()
        {
            if (!ConnectionOpen)
                throw new InvalidOperationException("Stream has closed!");

            while (true)
            {
                string line = await _reader.ReadLineAsync();

                if (line is null)
                {
                    OnStreamClosed();
                    return;
                }

                if (await TryHandleResponseAsync(line))
                    return;

                if (await TryHandleCommandAsync(line))
                    return;

                // Respond with "WHATYOUMEAN??"
                await SendAsync(new Response
                {
                    Code = ResponseCode.SyntaxError,
                    Message = "Syntax error: Unable to parse message"
                });
            }
        }

        public async Task SendAsync(Response response)
        {
            if (!ConnectionOpen)
                throw new InvalidOperationException("Stream has closed!");

            using (_writerSemaphore.EnterAsync())
            {
                await _writer.WriteLineAsync(response.ToString());
                await _writer.FlushAsync();
            }
        }

        public async Task SendAsync([NotNull] string command, [CanBeNull] string argument)
        {
            if (command is null)
                throw new ArgumentNullException(nameof(command));

            ICommandTemplate commandTemplate = GetCommand(command);

            if (commandTemplate is null)
                throw new NotSupportedException($"Command {command.ToUpper()} is not registered for this stream.");

            await SendAsyncInternal(commandTemplate, argument);
        }

        public async Task SendAsync([NotNull] ICommandTemplate commandTemplate, [CanBeNull] string argument)
        {
            if (commandTemplate is null)
                throw new ArgumentNullException(nameof(commandTemplate));

            if (!_registeredCommands.Contains(commandTemplate))
                throw new NotSupportedException(
                    $"Command {commandTemplate.Command.ToUpper()} is not registered for this stream.");

            if (!ConnectionOpen)
                throw new InvalidOperationException("Stream has closed!");

            await SendAsyncInternal(commandTemplate, argument);
        }

        private async Task SendAsyncInternal([NotNull] ICommandTemplate commandTemplate, [CanBeNull] string argument)
        {
            using (_writerSemaphore.EnterAsync())
            {
                await _writer.WriteLineAsync(string.IsNullOrEmpty(argument)
                    ? commandTemplate.Command
                    : $"{commandTemplate.Command} {argument}");

                await _writer.FlushAsync();
            }
        }

        [Pure, CanBeNull]
        public ICommandTemplate GetCommand([NotNull] string command)
        {
            return _registeredCommands.FirstOrDefault(cmd =>
                cmd.Command.Equals(command, StringComparison.InvariantCultureIgnoreCase));
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

            if (!_registeredCommands.Contains(commandTemplate))
                throw new ArgumentException(
                    $"Command {commandTemplate.Command.ToUpper()} is already registered.",
                    nameof(commandTemplate));

            _registeredCommands.Add(commandTemplate);
        }

        #endregion

        public void Dispose()
        {
            _stream.Dispose();
            _reader.Dispose();
            _writer.Dispose();
            _writerSemaphore.Dispose();
        }

        protected virtual void OnCommandReceived(ReceivedCommand e)
        {
            foreach (IObserver<IPacket> observer in _packetObservers)
            {
                observer.OnNext(e);
            }
        }

        protected virtual void OnResponseReceived(Response e)
        {
            foreach (IObserver<IPacket> observer in _packetObservers)
            {
                observer.OnNext(e);
            }
        }

        protected virtual void OnStreamClosed()
        {
            ConnectionOpen = false;
            StreamClosed?.Invoke(this, EventArgs.Empty);
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