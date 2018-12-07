using JetBrains.Annotations;

namespace BattleshipProtocol.Protocol
{
    public struct Response
    {
        public ResponseCode Code { get; set; }

        [CanBeNull]
        public string Message { get; set; }
    }
}