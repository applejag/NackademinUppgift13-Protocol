using BattleshipProtocol.Protocol.Exceptions;
using JetBrains.Annotations;

namespace BattleshipProtocol.Protocol.Internal.Extensions
{
    internal static class BattleGameExtensions
    {
        public static void ThrowIfHost([NotNull] this BattleGame game, [NotNull] string commandName)
        {
            if (game.IsHost)
                throw new ProtocolBoundedCommandException(commandName, "client");
        }

        public static void ThrowIfHost([NotNull] this BattleGame game, ResponseCode responseCode)
        {
            if (game.IsHost)
                throw new ProtocolBoundedResponseException(responseCode, "client");
        }

        public static void ThrowIfNotHost([NotNull] this BattleGame game, [NotNull] string commandName)
        {
            if (!game.IsHost)
                throw new ProtocolBoundedCommandException(commandName, "host");
        }

        public static void ThrowIfNotHost([NotNull] this BattleGame game, ResponseCode responseCode)
        {
            if (!game.IsHost)
                throw new ProtocolBoundedResponseException(responseCode, "host");
        }

        public static void ThrowIfWrongState([NotNull] this BattleGame game, [NotNull] string commandName, GameState expected)
        {
            if (game.GameState != expected)
                throw new ProtocolInvalidStateCommandException(commandName, expected, game.GameState);
        }

        public static void ThrowIfWrongState([NotNull] this BattleGame game, ResponseCode responseCode, GameState expected)
        {
            if (game.GameState != expected)
                throw new ProtocolInvalidStateResponseException(responseCode, expected, game.GameState);
        }

        public static void ThrowIfLocalsTurn([NotNull] this BattleGame game)
        {
            if (game.IsLocalsTurn)
                throw new ProtocolPlayerTurnException(true);
        }

        public static void ThrowIfNotLocalsTurn([NotNull] this BattleGame game)
        {
            if (!game.IsLocalsTurn)
                throw new ProtocolPlayerTurnException(false);
        }
    }
}