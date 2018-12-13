using System;
using JetBrains.Annotations;

namespace BattleshipProtocol.Protocol.Exceptions
{
    public class ProtocolException : Exception
    {
        public ResponseCode ErrorCode { get; }
        
        [NotNull]
        public string ErrorMessage { get; }

        public ProtocolException(ResponseCode errorCode, [NotNull] string errorMessage)
            : this(errorCode, errorMessage, null)
        { }

        public ProtocolException(ResponseCode errorCode, [NotNull] string errorMessage, [CanBeNull] Exception innerException)
            : this($"Encountered protocol exception {(short)errorCode} {errorCode.ToString()}: {errorMessage}",
                errorCode, errorMessage, innerException)
        { }

        internal ProtocolException([NotNull] string message, ResponseCode errorCode,
            [NotNull] string errorMessage, [CanBeNull] Exception innerException)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
        }
    }
}