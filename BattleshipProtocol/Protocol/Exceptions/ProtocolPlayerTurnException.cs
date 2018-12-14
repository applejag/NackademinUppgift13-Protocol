using System;

namespace BattleshipProtocol.Protocol.Exceptions
{
    public class ProtocolPlayerTurnException : ProtocolException
    {
        public bool ExpectedLocalsPlayerTurn { get; }

        public ProtocolPlayerTurnException(bool expectedLocalsPlayerTurn)
            : base(ResponseCode.SequenceError, $"Sequence error: Not your turn.")
        {
            ExpectedLocalsPlayerTurn = expectedLocalsPlayerTurn;
        }
    }
}