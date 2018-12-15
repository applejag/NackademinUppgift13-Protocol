namespace BattleshipProtocol.Protocol.Exceptions
{
    public class ProtocolTooManyErrorsException : ProtocolException
    {
        public int ErrorCount { get; set; }

        public ProtocolTooManyErrorsException(int errorCount)
            : base(ResponseCode.SequenceError, $"Sequence error: Received {errorCount} consecutive errors. Disconnecting...")
        {
            ErrorCount = errorCount;
        }
    }
}