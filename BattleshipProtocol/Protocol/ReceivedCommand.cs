using BattleshipProtocol.Protocol.Commands;
using JetBrains.Annotations;

namespace BattleshipProtocol.Protocol
{
    public struct ReceivedCommand : IPacket
    {
        public string Source { get; set; }

        [NotNull]
        public ICommandTemplate CommandTemplate { get; set; }

        [CanBeNull]
        public string Argument { get; set; }
    }
}