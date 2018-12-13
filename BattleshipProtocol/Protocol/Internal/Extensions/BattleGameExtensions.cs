using BattleshipProtocol.Protocol.Exceptions;
using JetBrains.Annotations;

namespace BattleshipProtocol.Protocol.Internal.Extensions
{
    internal static class BattleGameExtensions
    {
        public static void ThrowIfHost([NotNull] this BattleGame game, [NotNull] string commandName)
        {
            if (game.IsHost)
                throw new ProtocolBoundedCommandException(commandName, "host");
        }

        public static void ThrowIfHost([NotNull] this BattleGame game, ResponseCode responseCode)
        {
            if (game.IsHost)
                throw new ProtocolBoundedResponseException(responseCode, "host");
        }

        public static void ThrowIfNotHost([NotNull] this BattleGame game, [NotNull] string commandName)
        {
            if (!game.IsHost)
                throw new ProtocolBoundedCommandException(commandName, "client");
        }

        public static void ThrowIfNotHost([NotNull] this BattleGame game, ResponseCode responseCode)
        {
            if (!game.IsHost)
                throw new ProtocolBoundedResponseException(responseCode, "client");
        }
    }
}