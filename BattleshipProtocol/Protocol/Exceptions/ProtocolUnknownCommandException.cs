using BattleshipProtocol.Protocol.Commands;
using JetBrains.Annotations;

namespace BattleshipProtocol.Protocol.Exceptions
{
    public class ProtocolUnknownCommandException : ProtocolException
    {
        [NotNull]
        public string UnknownCommand { get; }

        public ProtocolUnknownCommandException([NotNull] string unknownCommand) 
            : base(ResponseCode.SyntaxError, $"Syntax error: Command {unknownCommand.ToUpperInvariant()} not found.")
        {
            UnknownCommand = unknownCommand;
        }
    }
}