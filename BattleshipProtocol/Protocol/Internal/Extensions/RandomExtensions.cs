using System;

namespace BattleshipProtocol.Protocol.Internal.Extensions
{
    internal static class RandomExtensions
    {
        public static bool NextBool(this Random random)
        {
            return random.Next(2) == 0;
        }
    }
}