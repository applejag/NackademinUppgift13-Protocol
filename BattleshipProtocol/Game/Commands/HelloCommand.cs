using System.Threading.Tasks;
using BattleshipProtocol.Protocol;
using BattleshipProtocol.Protocol.Exceptions;
using BattleshipProtocol.Protocol.Internal.Extensions;

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
        public async Task OnCommandAsync(PacketConnection context, string argument)
        {
            _game.ThrowIfNotHost(Command);
            _game.ThrowIfWrongState(Command, GameState.Handshake);

            SetNameFromArgument(argument);

            await context.SendResponseAsync(ResponseCode.Handshake, _game.LocalPlayer.Name);
            _game.GameState = GameState.Idle;
        }

        /// <inheritdoc />
        public Task OnResponseAsync(PacketConnection context, Response response)
        {
            _game.ThrowIfHost(response.Code);
            _game.ThrowIfWrongState(response.Code, GameState.Handshake);

            SetNameFromArgument(response.Message);
            _game.GameState = GameState.Idle;
            return Task.CompletedTask;
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