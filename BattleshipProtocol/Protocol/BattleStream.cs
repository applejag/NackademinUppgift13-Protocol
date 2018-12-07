using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BattleshipProtocol.Protocol.Commands;
using JetBrains.Annotations;

namespace BattleshipProtocol.Protocol
{
    public class BattleStream : IDisposable
    {
        private readonly Stream _stream;
        private readonly StreamReader _reader;
        private readonly StreamWriter _writer;

        private readonly Regex _commandRegex = new Regex(@"^([a-z]+)(?: (.*))?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex _responseRegex = new Regex(@"^([0-9]{1,3})(?: (.*))?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public bool ConnectionOpen { get; private set; }

        [NotNull] [ItemNotNull]
        private readonly List<ICommand> _registeredCommands = new List<ICommand>();

        [NotNull, ItemNotNull]
        public IReadOnlyCollection<ICommand> RegisteredCommands => _registeredCommands;

        public event EventHandler<ICommand> CommandReceived;
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

        /// <summary>
        /// Waits and receives one response or command.
        /// To catch these messages, see <see cref="CommandReceived"/> and <see cref="ResponseReceived"/>
        /// </summary>
        public void Receive()
        {
            if (!ConnectionOpen)
                throw new InvalidOperationException("Stream has closed!");

            while (true)
            {
                string line = _reader.ReadLine();

                if (line is null)
                {
                    OnStreamClosed();
                    return;
                }

                // Check if response code
                Match match = _responseRegex.Match(line);

                if (match.Success)
                {
                    var code = (ResponseCode)short.Parse(match.Groups[1].Value);

                    // Validate
                    if (!Enum.IsDefined(typeof(ResponseCode), code))
                    {
                        Send( new Response
                        {
                            Code = ResponseCode.SyntaxError,
                            Message = $"Syntax error: Unknown response code {code}"
                        });

                        continue;
                    }

                    // Register response received
                    OnResponseReceived(new Response
                    {
                        Code = code,
                        Message = match.Groups[2].Value
                    });
                    return;
                }

                // Check if command
                match = _commandRegex.Match(line);

                if (match.Success)
                {
                    string commandCode = match.Groups[1].Value;
                    ICommand command = GetCommand(commandCode, match.Groups[2].Value);

                    // Validate
                    if (command is null)
                    {
                        Send(new Response
                        {
                            Code = ResponseCode.SyntaxError,
                            Message = $"Syntax error: Command \"{commandCode.ToUpperInvariant()}\" not found"
                        });
                        continue;
                    }

                    // Register received command
                    OnCommandReceived(command);
                    return;
                }

                // Respond with "WHATYOUMEAN??"
                Send(new Response
                {
                    Code = ResponseCode.SyntaxError,
                    Message = "Syntax error: Unable to parse message"
                });

            }
        }

        public void Send(Response response)
        {
            if (!ConnectionOpen)
                throw new InvalidOperationException("Stream has closed!");

            lock (_writer)
            {
                _writer.WriteLine(response.ToString());
                _writer.Flush();
            }
        }

        public void Send([NotNull] ICommand command)
        {
            if (command is null)
                throw new ArgumentNullException(nameof(command));

            if (!ConnectionOpen)
                throw new InvalidOperationException("Stream has closed!");

            lock (_writer)
            {
                _writer.WriteLine(string.IsNullOrEmpty(command.Message)
                    ? command.Command
                    : $"{command.Command} {command.Message}");

                _writer.Flush();
            }
        }

        [Pure, CanBeNull]
        public ICommand GetCommand(string command, string argument)
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
        }

        protected virtual void OnCommandReceived([NotNull] ICommand e)
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