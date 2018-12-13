using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace BattleshipProtocol.Protocol
{
    public interface ICommandTemplate
    {
        /// <summary>
        /// The ~4-5 character code of this command.
        /// </summary>
        [NotNull]
        string Command { get; }

        /// <summary>
        /// Which response codes that shall be routed to this command.
        /// </summary>
        [NotNull]
        ResponseCode[] RoutedResponseCodes { get; }

        /// <summary>
        /// Handles a received command from the other client.
        /// </summary>
        /// <param name="context">The game context.</param>
        /// <param name="argument">The argument received with the command.</param>
        Task OnCommandAsync([NotNull] PacketConnection context, [CanBeNull] string argument);

        /// <summary>
        /// Handles a received response from the other client.
        /// </summary>
        /// <param name="context">The game context.</param>
        /// <param name="response">The response.</param>
        Task OnResponseAsync([NotNull] PacketConnection context, Response response);
    }
}