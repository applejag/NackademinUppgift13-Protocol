using System.Threading.Tasks;
using BattleshipProtocol.Protocol;
using BattleshipProtocol.Protocol.Exceptions;

namespace BattleshipProtocol.Game.Commands
{
    public class HelloCommand : ICommandTemplate
    {
        /// <inheritdoc />
        public string Command { get; } = "HELO";

        /// <inheritdoc />
        public ResponseCode[] RoutedResponseCodes { get; } =
        {
            ResponseCode.Handshake
        };

        private readonly BattleGame _game;

        public HelloCommand(BattleGame game)
        {
            _game = game;
        }

        /// <inheritdoc />
        public async void OnCommand(PacketConnection context, string argument)
        {
            SetNameFromArgument(argument);

            await context.SendResponseAsync(ResponseCode.Handshake, _game.LocalPlayer.Name);
        }

        /// <inheritdoc />
        public void OnResponse(PacketConnection context, Response response)
        {
            SetNameFromArgument(response.Message);
        }

        private void SetNameFromArgument(string argument)
        {
            if (_game.RemotePlayer.Name != null)
                throw new ProtocolException(ResponseCode.SequenceError, "Name already set.");

            argument = argument?.Trim();

            if (string.IsNullOrEmpty(argument))
                throw new ProtocolException(ResponseCode.SyntaxError, "Missing name from response.");

            _game.RemotePlayer.Name = argument;
        }
    }
}