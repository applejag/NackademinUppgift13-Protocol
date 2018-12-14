using System;
using System.Threading.Tasks;
using BattleshipProtocol.Protocol;
using BattleshipProtocol.Protocol.Internal.Extensions;

namespace BattleshipProtocol.Game.Commands
{
    public class StartCommand : ICommandTemplate
    {
        /// <inheritdoc />
        public string Command { get; } = "START";

        /// <inheritdoc />
        public ResponseCode[] RoutedResponseCodes { get; } =
        {
            ResponseCode.StartClient,
            ResponseCode.StartHost
        };

        private readonly BattleGame _game;
        private readonly Random _random = new Random();

        public StartCommand(BattleGame game)
        {
            _game = game;
        }

        /// <inheritdoc />
        public Task OnCommandAsync(PacketConnection context, string argument)
        {
            _game.ThrowIfNotHost(Command);
            _game.ThrowIfWrongState(Command, GameState.Idle);

            _game.GameState = GameState.InGame;
            _game.IsLocalsTurn = _random.NextBool();

            if (_game.IsLocalsTurn)
                return context.SendResponseAsync(ResponseCode.StartHost, "Host starts");
            else
                return context.SendResponseAsync(ResponseCode.StartClient, "Client starts");
        }

        /// <inheritdoc />
        public Task OnResponseAsync(PacketConnection context, Response response)
        {
            _game.ThrowIfHost(response.Code);
            _game.ThrowIfWrongState(response.Code, GameState.Idle);

            _game.GameState = GameState.InGame;
            _game.IsLocalsTurn = response.Code == ResponseCode.StartClient;

            return Task.CompletedTask;
        }
    }
}