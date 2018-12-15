using System;

namespace BattleshipProtocol.Protocol.Exceptions
{
    public abstract class ProtocolInvalidStateException : ProtocolException
    {
        public GameState ExpectedState { get; }
        public GameState ActualState { get; }

        protected ProtocolInvalidStateException(string middleMessage, GameState expectedState, GameState actualState)
            : base(ResponseCode.SequenceError, $"Sequence error: {middleMessage} cannot be sent while {GetStateName(actualState)}")
        {
            ExpectedState = expectedState;
            ActualState = actualState;
        }

        protected static string GetStateName(GameState state)
        {
            switch (state)
            {
                case GameState.Handshake:
                    return "awaiting handshake";
                case GameState.Idle:
                    return "awaiting game";
                case GameState.InGame:
                    return "in-game";
                case GameState.Disconnected:
                    return "disconnected";
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }
    }
}