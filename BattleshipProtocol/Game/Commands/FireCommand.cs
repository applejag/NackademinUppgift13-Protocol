using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BattleshipProtocol.Protocol;
using BattleshipProtocol.Protocol.Exceptions;
using BattleshipProtocol.Protocol.Internal.Extensions;
using JetBrains.Annotations;

namespace BattleshipProtocol.Game.Commands
{
    public class FireCommand : ICommandTemplate
    {
        /// <inheritdoc />
        public string Command { get; } = "FIRE";

        public ResponseCode[] RoutedResponseCodes { get; } =
        {
            ResponseCode.FireMiss,

            ResponseCode.FireHitCarrier,
            ResponseCode.FireHitBattleship,
            ResponseCode.FireHitDestroyer,
            ResponseCode.FireHitSubmarine,
            ResponseCode.FireHitPatrolBoat,

            ResponseCode.FireSunkCarrier,
            ResponseCode.FireSunkBattleship,
            ResponseCode.FireSunkDestroyer,
            ResponseCode.FireSunkSubmarine,
            ResponseCode.FireSunkPatrolBoat,

            ResponseCode.FireYouWin,
        };

        private readonly BattleGame _game;
        private static readonly Regex _argRegex = new Regex(@"^\s*(\w+)(?:\s+(.+?))?\s*$",
            RegexOptions.Compiled);

        public Coordinate? WaitingForResponseAt { get; set; }

        public FireCommand(BattleGame game)
        {
            _game = game;
        }

        /// <inheritdoc />
        public Task OnCommandAsync(PacketConnection context, string argument)
        {
            _game.ThrowIfWrongState(Command, GameState.InGame);
            _game.ThrowIfLocalsTurn();
            if (WaitingForResponseAt.HasValue)
                throw new ProtocolException(ResponseCode.SequenceError, $"Sequence error: Awaiting FIRE response for {WaitingForResponseAt}");

            Coordinate coordinate = GetCoordinate(argument);

            Task responseTask = FireAndSendResponse(context, coordinate);
            _game.IsLocalsTurn = true;
            return responseTask;
        }

        /// <inheritdoc />
        public Task OnResponseAsync(PacketConnection context, Response response)
        {
            _game.ThrowIfWrongState(response.Code, GameState.InGame);
            _game.ThrowIfNotLocalsTurn();

            if (WaitingForResponseAt is null)
                throw new ProtocolException(ResponseCode.SequenceError, $"Sequence error: Awaiting {Command} command.");

            RegisterShot(WaitingForResponseAt.Value, response);


            return Task.CompletedTask;
        }

        private Task FireAndSendResponse(PacketConnection context, Coordinate coordinate)
        {
            if (_game.LocalPlayer.Board.IsShotAt(in coordinate))
                throw new ProtocolException(ResponseCode.SequenceError, $"Sequence error: {coordinate} has already been fired upon.");

            Ship ship = _game.LocalPlayer.Board.ShootAtInternal(coordinate);

            if (ship is null)
                return context.SendResponseAsync(ResponseCode.FireMiss, "Miss!");

            if (ship.Health > 0)
                return context.SendResponseAsync(GetResponseCode(ship.Type, sunk: false), $"You hit my {ship.Name}");

            if (_game.LocalPlayer.Board.Ships.All(s => s.Health == 0))
            {
                // Game over
                _game.GameState = GameState.Idle;
                return context.SendResponseAsync(ResponseCode.FireYouWin, "You win!");
            }

            return context.SendResponseAsync(GetResponseCode(ship.Type, sunk: true), $"You sunk my {ship.Name}");
        }

        private Coordinate GetCoordinate(string argument)
        {
            Match argMatch = _argRegex.Match(argument ?? string.Empty);

            if (!argMatch.Success)
                throw new ProtocolArgumentMissingException(Command);

            string coordinateStr = argMatch.Groups[1].Value;
            string message = argMatch.Groups[2].Value;

            Coordinate coordinate;
            try
            {
                coordinate = Coordinate.Parse(coordinateStr);
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new ProtocolException(ResponseCode.SyntaxError,
                    $"Syntax error: Coordinate {coordinateStr} is outside the playing field.");
            }
            catch (Exception)
            {
                throw new ProtocolException(ResponseCode.SyntaxError, "Syntax error: Unable to parse coordinate.");
            }

            return coordinate;
        }

        private void RegisterShot(in Coordinate coordinate, Response response)
        {
            if (_game.RemotePlayer.Board.IsShotAt(in coordinate))
            {
                WaitingForResponseAt = null;
                _game.IsLocalsTurn = false;

                throw new ProtocolException(ResponseCode.SequenceError,
                    $"Sequence error: {WaitingForResponseAt} was already fired upon. Ignores response.");
            }

            switch (response.Code)
            {
                case ResponseCode.FireYouWin:
                    {
                        _game.GameState = GameState.Idle;

                        Ship[] lastShips = _game.RemotePlayer.Board.Ships.Where(s => s.Health > 0).ToArray();

                        // No ships lets?
                        if (lastShips.Length == 0)
                            throw new ProtocolException(ResponseCode.SequenceError,
                                $"Sequence error: Received {(short)ResponseCode.FireYouWin}, but there were no ships remaining.");

                        // Cant evaluate which ship got shot
                        if (lastShips.Length > 1)
                            throw new ProtocolException(ResponseCode.SequenceError,
                                $"Sequence error: Received {(short)ResponseCode.FireYouWin}, but there are more than one ship remaining.");

                        _game.RemotePlayer.Board.RegisterShot(in coordinate, lastShips[0].Type);

                        WaitingForResponseAt = null;
                        _game.IsLocalsTurn = false;
                        break;
                    }
                case ResponseCode.FireMiss:
                    _game.RemotePlayer.Board.RegisterShot(in coordinate, null);

                    WaitingForResponseAt = null;
                    _game.IsLocalsTurn = false;
                    break;
                default:
                    {
                        // Response was a hit or sunk
                        Ship ship = _game.RemotePlayer.Board.RegisterShot(in coordinate, GetShipType(response.Code));

                        WaitingForResponseAt = null;
                        _game.IsLocalsTurn = false;

                        if (_game.RemotePlayer.Board.Ships.All(s => s.Health == 0))
                        {
                            _game.GameState = GameState.Idle;

                            throw new ProtocolException(ResponseCode.SequenceError,
                                $"Sequence error: Last ship was sunk but no {(short)ResponseCode.FireYouWin}. Assuming victory.");
                        }

                        bool isSunkCode = IsSunkCode(response.Code);
                        if (ship?.Health > 0 && isSunkCode)
                        {
                            throw new ProtocolException(ResponseCode.SequenceError,
                                $"Sequence error: Received ship sunk but ship has {ship.Health} health left. Assuming hit.");
                        }

                        if (ship?.Health == 0 && !isSunkCode)
                        {
                            throw new ProtocolException(ResponseCode.SequenceError,
                                $"Sequence error: Received ship hit but ship has {ship.Health} health left. Assuming sunk.");
                        }

                        break;
                    }
            }
        }

        private static bool IsSunkCode(ResponseCode responseCode)
        {
            switch (responseCode)
            {
                case ResponseCode.FireSunkCarrier:
                case ResponseCode.FireSunkBattleship:
                case ResponseCode.FireSunkDestroyer:
                case ResponseCode.FireSunkSubmarine:
                case ResponseCode.FireSunkPatrolBoat:
                    return true;
            }

            return false;
        }

        [Pure]
        private static ShipType GetShipType(ResponseCode responseCode)
        {
            switch (responseCode)
            {
                case ResponseCode.FireHitCarrier:
                case ResponseCode.FireSunkCarrier:
                    return ShipType.Carrier;
                case ResponseCode.FireHitBattleship:
                case ResponseCode.FireSunkBattleship:
                    return ShipType.Battleship;
                case ResponseCode.FireHitDestroyer:
                case ResponseCode.FireSunkDestroyer:
                    return ShipType.Destroyer;
                case ResponseCode.FireHitSubmarine:
                case ResponseCode.FireSunkSubmarine:
                    return ShipType.Submarine;
                case ResponseCode.FireHitPatrolBoat:
                case ResponseCode.FireSunkPatrolBoat:
                    return ShipType.PatrolBoat;

                default:
                    throw new ArgumentOutOfRangeException(nameof(responseCode), responseCode, null);
            }
        }

        [Pure]
        private static ResponseCode GetResponseCode(ShipType shipType, bool sunk)
        {
            if (sunk)
            {
                switch (shipType)
                {
                    case ShipType.Carrier: return ResponseCode.FireSunkCarrier;
                    case ShipType.Battleship: return ResponseCode.FireSunkBattleship;
                    case ShipType.Destroyer: return ResponseCode.FireSunkDestroyer;
                    case ShipType.Submarine: return ResponseCode.FireSunkSubmarine;
                    case ShipType.PatrolBoat: return ResponseCode.FireSunkPatrolBoat;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(shipType), shipType, null);
                }
            }

            switch (shipType)
            {
                case ShipType.Carrier: return ResponseCode.FireHitCarrier;
                case ShipType.Battleship: return ResponseCode.FireHitBattleship;
                case ShipType.Destroyer: return ResponseCode.FireHitDestroyer;
                case ShipType.Submarine: return ResponseCode.FireHitSubmarine;
                case ShipType.PatrolBoat: return ResponseCode.FireHitPatrolBoat;

                default:
                    throw new ArgumentOutOfRangeException(nameof(shipType), shipType, null);
            }
        }
    }
}