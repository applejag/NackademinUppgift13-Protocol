using BattleshipProtocol.Protocol.Commands;
using JetBrains.Annotations;

namespace BattleshipProtocol.Protocol.Exceptions
{
    public class ProtocolFormatException : ProtocolException
    {
        public ProtocolFormatException()
            : base(ResponseCode.SyntaxError, $"Syntax error: Unable to parse packet.")
        { }
    }
}