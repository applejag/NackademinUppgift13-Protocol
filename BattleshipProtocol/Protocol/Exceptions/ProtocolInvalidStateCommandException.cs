using System;

namespace BattleshipProtocol.Protocol.Exceptions
{
    public class ProtocolInvalidStateCommandException : ProtocolInvalidStateException
    {
        public string Command { get; }

        public ProtocolInvalidStateCommandException(string command, GameState expectedState, GameState actualState)
            : base($"Command {command.ToUpperInvariant()}", expectedState, actualState)
        {
            Command = command;
        }
    }
}