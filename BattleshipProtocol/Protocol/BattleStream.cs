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
using JetBrains.Annotations;

namespace BattleshipProtocol.Protocol
{
    public class BattleStream : IDisposable
    {
        private readonly Stream _stream;
        private readonly StreamReader _reader;
        private readonly StreamWriter _writer;

        private readonly SemaphoreSlim _writerSemaphore = new SemaphoreSlim(1, 1);

        private readonly Regex _commandRegex = new Regex(@"^([a-z]+)(?: (.*))?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex _responseRegex = new Regex(@"^([0-9]{1,3})(?: (.*))?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public bool ConnectionOpen { get; private set; }

        [NotNull]
        [ItemNotNull]
        private readonly List<ICommand> _registeredCommands = new List<ICommand>();

        [NotNull, ItemNotNull]
        public IReadOnlyCollection<ICommand> RegisteredCommands => _registeredCommands;

        public event EventHandler<ReceivedCommand> CommandReceived;
        public event EventHandler<Response> ResponseReceived;
        public event EventHandler StreamClosed;

        /// <inheritdoc />
        /// <summary>
        /// Initializes the Battleship commands stream with encoding <see cref="P:System.Text.Encoding.UTF8" />
        /// </summary>
        /// <param name="stream">The stream to use when reading and writing data.</param>
        public BattleStream([NotNull] Stream stream)
            : this(stream, Encoding.UTF8)
        { }

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

        private async Task<bool> TryHandleResponseAsync(string value)
        {
            // Check if response code
            Match match = _responseRegex.Match(value);

            if (!match.Success)
                return false;

            var code = (ResponseCode)short.Parse(match.Groups[1].Value);

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
                Code = code,
                Message = match.Groups[2].Value
            });
            return true;
        }

        private async Task<bool> TryHandleCommandAsync(string value)
        {
            // Check if command
            Match match = _commandRegex.Match(value);

            if (!match.Success)
                return false;

            string commandCode = match.Groups[1].Value;
            ICommand command = GetCommand(commandCode);

            // Validate
            if (command is null)
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
                Command = command,
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

        public async Task SendAsync([NotNull] ICommand command, [CanBeNull] string argument)
        {
            if (command is null)
                throw new ArgumentNullException(nameof(command));

            if (!ConnectionOpen)
                throw new InvalidOperationException("Stream has closed!");

            using (_writerSemaphore.EnterAsync())
            {
                await _writer.WriteLineAsync(string.IsNullOrEmpty(argument)
                    ? command.Command
                    : $"{command.Command} {argument}");

                await _writer.FlushAsync();
            }
        }

        [Pure, CanBeNull]
        public ICommand GetCommand([NotNull] string command)
        {
            return _registeredCommands.FirstOrDefault(cmd =>
                cmd.Command.Equals(command, StringComparison.InvariantCultureIgnoreCase));
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
            CommandReceived?.Invoke(this, e);
        }

        protected virtual void OnResponseReceived(Response e)
        {
            ResponseReceived?.Invoke(this, e);
        }

        protected virtual void OnStreamClosed()
        {
            ConnectionOpen = false;
            StreamClosed?.Invoke(this, EventArgs.Empty);
        }
    }
}