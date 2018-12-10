using JetBrains.Annotations;

namespace BattleshipProtocol.Protocol
{
    public interface IPacket
    {
        [NotNull]
        string Source { get; }
    }
}