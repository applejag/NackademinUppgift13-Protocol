using JetBrains.Annotations;

namespace BattleshipProtocol.Protocol
{
    public struct Response : IPacket
    {
        public string Source { get; set; }

        public ResponseCode Code { get; set; }

        [CanBeNull]
        public string Message { get; set; }

        public Response(ResponseCode code, string message)
        {
            Code = code;
            Message = message;
            Source = string.IsNullOrEmpty(message)
                ? $"{(short)code}"
                : $"{(short)code} {message}";
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(Message)
                ? $"{(short) Code}"
                : $"{(short) Code} {Message}";
        }
    }
}