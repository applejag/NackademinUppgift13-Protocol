namespace BattleshipProtocol.Protocol.Exceptions
{
    public class ProtocolArgumentMissingException : ProtocolException
    {
        public string Command { get; }

        public ProtocolArgumentMissingException(string command)
            : base(ResponseCode.SyntaxError, $"Syntax error: Missing argument for {command.ToUpperInvariant()}")
        {
            Command = command;
        }
    }
}