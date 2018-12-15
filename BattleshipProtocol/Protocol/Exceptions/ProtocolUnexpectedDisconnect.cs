namespace BattleshipProtocol.Protocol.Exceptions
{
    public class ProtocolUnexpectedDisconnect : ProtocolException
    {
        public ProtocolUnexpectedDisconnect() 
            : base(ResponseCode.SequenceError, "Sequence error: Unexpected disconnect from remote.")
        {
        }
    }
}