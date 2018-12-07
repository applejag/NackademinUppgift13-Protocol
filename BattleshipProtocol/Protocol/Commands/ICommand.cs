
using JetBrains.Annotations;

namespace BattleshipProtocol.Protocol.Commands
{
    public interface ICommand
    {
        /// <summary>
        /// The ~4-5 character code of this command.
        /// </summary>
        [NotNull]
        string Command { get; }

        /// <summary>
        /// The additional optional argument sent with the command. Leave empty or null to omit.
        /// </summary>
        [CanBeNull]
        string Message { get; set; }

        /// <summary>
        /// Handles a received command from the other client.
        /// </summary>
        /// <param name="context"></param>
        void HandleCommand(BattleGame context);

        /// <summary>
        /// Handles a received response from the other client.
        /// </summary>
        /// <param name="context">The game context.</param>
        /// <param name="response">The response.</param>
        void HandleResponse(BattleGame context, Response response);
    }
}