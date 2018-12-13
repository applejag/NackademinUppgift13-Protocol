using JetBrains.Annotations;

namespace BattleshipProtocol.Protocol.Exceptions
{
    /// <summary>
    /// Represents an error for when a command or response is received by a server/host, but only available to a client
    /// </summary>
    public class ProtocolBoundedResponseException : ProtocolException
    {
        public ResponseCode LockedResponse { get; set; }

        public ProtocolBoundedResponseException(ResponseCode lockedResponseCode, string bound)
            : base(ResponseCode.SyntaxError, $"Syntax error: Response code {(short)lockedResponseCode} cannot be sent by a {bound}.")
        {
            LockedResponse = lockedResponseCode;
        }
    }
}