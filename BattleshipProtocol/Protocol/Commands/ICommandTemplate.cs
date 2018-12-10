using JetBrains.Annotations;

namespace BattleshipProtocol.Protocol.Commands
{
    public interface ICommandTemplate
    {
        /// <summary>
        /// The ~4-5 character code of this command.
        /// </summary>
        [NotNull]
        string Command { get; }

        /// <summary>
        /// Handles a received command from the other client.
        /// </summary>
        /// <param name="context">The game context.</param>
        /// <param name="argument">The argument received with the command.</param>
        void OnCommand([NotNull] BattleGame context, [CanBeNull] string argument);

        /// <summary>
        /// Handles a received response from the other client.
        /// </summary>
        /// <param name="context">The game context.</param>
        /// <param name="response">The response.</param>
        void OnResponse([NotNull] BattleGame context, Response response);
    }
}