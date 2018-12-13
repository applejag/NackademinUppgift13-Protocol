using System;
using System.Text;
using System.Threading.Tasks;
using BattleshipProtocol.Protocol;

namespace BattleshipProtocol.Game.Commands
{
    public class HelpCommand : ICommandTemplate
    {
        /// <inheritdoc />
        public string Command { get; } = "HELP";

        /// <inheritdoc />
        public ResponseCode[] RoutedResponseCodes { get; } = { };

        /// <inheritdoc />
        public async Task OnCommandAsync(PacketConnection context, string argument)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Current version: {BattleGame.ProtocolVersion}");
            sb.AppendLine("## Available command:");
            sb.AppendLine("- START");
            sb.AppendLine("\tStarts a new game session.");
            sb.AppendLine("\tSent by: client");
            sb.AppendLine($"\tResponse {221}: Client has first turn");
            sb.AppendLine($"\tResponse {222}: Host has first turn");
            sb.AppendLine();
            sb.AppendLine("- FIRE <coordinate> [message]");
            sb.AppendLine("\tFire at a coordinate, and give optional message.");
            sb.AppendLine("\tCoordinate shall be formatted <letter><number>.");
            sb.AppendLine("\tCoordinate ranges from A1 to J10.");
            sb.AppendLine("\tSent by: both");
            sb.AppendLine($"\tResponse {230}: you missed");
            sb.AppendLine($"\tResponse {241}-{245}: you hit a ship");
            sb.AppendLine($"\tResponse {251}-{255}: you sunk a ship");
            sb.AppendLine($"\tResponse {260}: you sunk the last ship, and won");
            sb.AppendLine();
            sb.AppendLine("- HELO <name>");
            sb.AppendLine("\tGreeting containing name of client.");
            sb.AppendLine("\tSent by: client");
            sb.AppendLine($"\tResponse {220} <name>: reply containing host name");
            sb.AppendLine();
            sb.AppendLine("- HELP");
            sb.AppendLine("\tSends this help text.");
            sb.AppendLine();
            sb.AppendLine("- QUIT");
            sb.AppendLine("\tOrders host to disconnect.");
            sb.AppendLine("\tExpect to be disconnected when response arrives.");
            sb.AppendLine("\tSent by: client");
            sb.AppendLine($"\tResponse {270}: host has closed connection");
            sb.AppendLine();
            sb.AppendLine("## Rules:");
            sb.AppendLine("\tBefore issuing START, prepare a 10x10 grid for");
            sb.AppendLine("\tyou and your opponent. Designate your 5 boats on");
            sb.AppendLine("\tyour grid.");
            sb.AppendLine();
            sb.AppendLine("\tWhen its your turn, send a FIRE at a coordinate not");
            sb.AppendLine("\tyet shot at. Record the result on your opponents grid.");
            sb.AppendLine("\tWhen your opponent sends a FIRE, if the coordinate is on");
            sb.AppendLine("\tone of your ships then mark your grid and respond with");
            sb.AppendLine("\tthe appropriate hit response code (241-255).");
            sb.AppendLine("\tSee table below.");
            sb.AppendLine();
            sb.AppendLine("\tWhen all coordinates of your ship is fired upon,");
            sb.AppendLine("\tthat ships is sunk, and shall give appropriate response");
            sb.AppendLine("\tcode (251-255). See table below.");
            sb.AppendLine("\tWhen all your ships are sunk, you lose, and shall give");
            sb.AppendLine("\tresponse code 240.");
            sb.AppendLine();
            sb.AppendLine("## Ships:");
            sb.AppendLine("\tList of the ships with their lengths and response codes.");
            sb.AppendLine();
            sb.AppendLine("\tName        | Length | Hit | Sunk");
            sb.AppendLine("\t------------|--------|-----|------");
            sb.AppendLine("\tCarrier     |      5 | 241 |  251");
            sb.AppendLine("\tBattleship  |      4 | 242 |  252");
            sb.AppendLine("\tDestroyer   |      3 | 243 |  253");
            sb.AppendLine("\tSubmarine   |      3 | 244 |  254");
            sb.AppendLine("\tPatrol boat |      2 | 245 |  255");
            sb.AppendLine();
            sb.AppendLine("Good luck, and have fun!");

            await context.SendTextAsync(sb.ToString());
        }

        /// <inheritdoc />
        public Task OnResponseAsync(PacketConnection context, Response response)
        {
            throw new NotSupportedException();
        }
    }
}