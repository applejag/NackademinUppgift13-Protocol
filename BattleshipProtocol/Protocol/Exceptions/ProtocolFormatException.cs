namespace BattleshipProtocol.Protocol.Exceptions
{
    public class ProtocolFormatException : ProtocolException
    {
        public string PacketSource { get; set; }

        public ProtocolFormatException(string packetSource)
            : base(ResponseCode.SyntaxError, $"Syntax error: Unable to parse packet.")
        {
            PacketSource = packetSource;
        }
    }
}