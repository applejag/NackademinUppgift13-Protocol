using JetBrains.Annotations;

namespace BattleshipProtocol.Protocol.Exceptions
{
    /// <summary>
    /// Represents an error for when a command or response is received by a client, but only available to a server/host
    /// </summary>
    public class ProtocolBoundedCommandException : ProtocolException
    {
        [NotNull]
        public string LockedCommand { get; set; }

        public ProtocolBoundedCommandException([NotNull] string lockedCommand, string bound)
            : base(ResponseCode.SyntaxError, $"Syntax error: Command {lockedCommand.ToUpperInvariant()} cannot be sent by a {bound}.")
        {
            LockedCommand = lockedCommand;
        }
    }
}