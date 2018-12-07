using BattleshipProtocol.Protocol.Commands;
using JetBrains.Annotations;

namespace BattleshipProtocol.Protocol
{
    public struct ReceivedCommand
    {
        [NotNull]
        public ICommand Command { get; set; }

        [CanBeNull]
        public string Argument { get; set; }
    }
}