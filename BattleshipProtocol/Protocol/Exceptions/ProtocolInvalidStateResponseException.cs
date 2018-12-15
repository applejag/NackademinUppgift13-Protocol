using System;

namespace BattleshipProtocol.Protocol.Exceptions
{
    public class ProtocolInvalidStateResponseException : ProtocolInvalidStateException
    {
        public ResponseCode ResponseCode { get; }

        public ProtocolInvalidStateResponseException(ResponseCode responseCode, GameState expectedState, GameState actualState)
            : base($"Response {(short)responseCode}", expectedState, actualState)
        {
            ResponseCode = responseCode;
        }
    }
}