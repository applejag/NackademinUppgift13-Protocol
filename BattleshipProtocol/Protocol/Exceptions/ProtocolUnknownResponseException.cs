using BattleshipProtocol.Protocol.Commands;
using JetBrains.Annotations;

namespace BattleshipProtocol.Protocol.Exceptions
{
    public class ProtocolUnknownResponseException : ProtocolException
    {
        public short UnknownResponseCode { get; }

        public ProtocolUnknownResponseException(short unknownCode) 
            : base(ResponseCode.SyntaxError, $"Syntax error: Unknown response code {unknownCode}")
        {
            UnknownResponseCode = unknownCode;
        }
    }
}