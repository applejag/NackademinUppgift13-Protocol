﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BattleshipProtocol.Protocol.Exceptions;
using BattleshipProtocol.Protocol.Internal;
using JetBrains.Annotations;

namespace BattleshipProtocol.Protocol
{
    public class PacketService : IObservable<IPacket>, IObserver<string>, IDisposable
    {
        private readonly Regex _commandRegex =
            new Regex(@"^([a-z]+)(?: (.*))?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly Regex _responseRegex =
            new Regex(@"^([0-9]{1,3})(?: (.*))?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly HashSet<ICommandTemplate> _registeredCommands = new HashSet<ICommandTemplate>();

        private readonly HashSet<IObserver<IPacket>> _packetObservers = new HashSet<IObserver<IPacket>>();

        private readonly IDisposable _streamUnsubscriber;
        private readonly StreamConnection _connection;

        [NotNull, ItemNotNull]
        public IReadOnlyCollection<ICommandTemplate> RegisteredCommands => _registeredCommands;
        public bool IsConnected => _connection.ConnectionOpen;

        internal PacketService(StreamConnection connection)
        {
            _streamUnsubscriber = connection.Subscribe(this);
            _connection = connection;
        }

        public void BeginListening()
        {
            _connection.BeginListening();
        }

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

        [NotNull]
        public Task<Response> ExpectResponseAsync(in int timeoutOverride)
        {
            int oldTimeout = _connection.ReadTimeout;
            _connection.ReadTimeout = timeoutOverride;

            try
            {
                return ExpectResponseAsync();
            }
            finally
            {
                _connection.ReadTimeout = oldTimeout;
            }
        }

        [NotNull]
        public async Task<Response> ExpectResponseAsync()
        {
            throw new NotImplementedException();
            //if (!ConnectionOpen)
            //    throw new InvalidOperationException("Stream has closed!");

            //if (_readerSemaphore.CurrentCount == 0)
            //    throw new InvalidOperationException("Stream has closed!");

            //using (await _readerSemaphore.EnterAsync())
            //{
            //    string line = await ReadLineAsyncInternal();

            //    if (line is null)
            //        throw new ProtocolUnexpectedDisconnect();

            //    if (!TryParseResponse(line, out Response response))
            //        throw new ProtocolFormatException(line);

            //    OnResponseReceived(response);
            //    return response;
            //}
        }

        [NotNull]
        public Task<ReceivedCommand> ExpectCommandAsync(in int timeoutOverride)
        {
            int oldTimeout = _connection.ReadTimeout;
            _connection.ReadTimeout = timeoutOverride;

            try
            {
                return ExpectCommandAsync();
            }
            finally
            {
                _connection.ReadTimeout = oldTimeout;
            }
        }

        [NotNull]
        public async Task<ReceivedCommand> ExpectCommandAsync()
        {
            throw new NotImplementedException();
            //if (!ConnectionOpen)
            //    throw new InvalidOperationException("Stream has closed!");

            //if (_readerSemaphore.CurrentCount == 0)
            //    throw new InvalidOperationException("Stream has closed!");

            //using (await _readerSemaphore.EnterAsync())
            //{
            //    string line = await ReadLineAsyncInternal();

            //    if (line is null)
            //        throw new ProtocolUnexpectedDisconnect();

            //    if (!TryParseCommand(line, out ReceivedCommand command))
            //        throw new ProtocolFormatException(line);

            //    OnCommandReceived(command);
            //    return command;
            //}
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

        /// <summary>
        /// Send a command (asynchronously) to the other client, with the optional argument.
        /// </summary>
        /// <param name="argument">The optional argument to append to the command. Should not contain newlines.</param>
        /// <exception cref="ArgumentException">Thrown if <typeparamref name="T"/> did not match any registered command.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the connection has been closed.</exception>
        [NotNull]
        public async Task SendCommandAsync<T>([CanBeNull] string argument = null) where T : class, ICommandTemplate
        {
            var commandTemplate = GetCommand<T>();

            if (commandTemplate is null)
                throw new ArgumentException($"Command {typeof(T).Name} is not registered for this stream.", nameof(T));

            await _connection.SendCommandAsync(commandTemplate, argument);
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

        private async void ParsePacketAsync(string packet)
        {
            try
            {
                if (TryParseResponse(in packet, out Response response))
                {
                    OnResponseReceived(in response);
                    return;
                }

                if (TryParseCommand(in packet, out ReceivedCommand command))
                {
                    OnCommandReceived(in command);
                    return;
                }

                // "WHAT YOU MEAN??"
                throw new ProtocolFormatException(packet);
            }
            catch (ProtocolException error)
            {
                await _connection.SendErrorAsync(error);
                OnError(error);
            }
            catch (Exception unexpected)
            {
                OnError(in unexpected);
            }
        }

        #region StreamConnection observing implementation

        void IObserver<string>.OnNext(string value)
        {
            ParsePacketAsync(value);
        }

        void IObserver<string>.OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        void IObserver<string>.OnCompleted()
        {
            throw new NotImplementedException();
        }

        #endregion

        public void Dispose()
        {
            _streamUnsubscriber.Dispose();
        }
    }
}