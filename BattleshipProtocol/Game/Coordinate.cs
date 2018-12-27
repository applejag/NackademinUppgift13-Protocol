using System;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace BattleshipProtocol.Game
{
    public struct Coordinate
    {
        private int _x;
        private int _y;
        public const string VerticalLetters = "ABCDEFGHIJ";

        private static readonly Regex ParseRegex = new Regex(@"(\d+)([a-z])|([a-z])(\d+)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        /// <summary>
        /// Gets or sets the X component of this coordinate. Ranges from 0 (west) to 9 (east).
        /// </summary>
        public int X {
            get => _x;
            set => _x = Validate(value);
        }

        /// <summary>
        /// Gets or sets the Y component of this coordinate. Ranges from 0 (north) to 9 (south).
        /// </summary>
        public int Y {
            get => _y;
            set => _y = Validate(value);
        }

        /// <summary>
        /// Gets or sets the indexed component of this coordinate. 0 for X and 1 for Y.
        /// </summary>
        /// <param name="index">The index.</param>
        public int this[int index]
        {
            get
            {
                if (index > 1 || index < 0)
                    throw new IndexOutOfRangeException();
                return index == 0 ? _x : _y;
            }
            set
            {
                if (index > 1 || index < 0)
                    throw new IndexOutOfRangeException();
                if (index == 0)
                    _x = Validate(value);
                else
                    _y = Validate(value);
            }
        }

        /// <summary>
        /// Gets the horizontal translated component of this coordinate. Ranges from 1 (west) to 9 (east).
        /// </summary>
        public int Horizontal => X + 1;

        /// <summary>
        /// Gets the vertical translated component of this coordinate. Ranges from A (north) to J (south).
        /// </summary>
        public char Vertical => VerticalLetters[Y];

        public Coordinate(int x, int y) : this()
        {
            X = x;
            Y = y;
        }

        [Pure]
        private static int Validate(in int input)
        {
            if (input > 9)
                throw new ArgumentOutOfRangeException(nameof(input));
            if (input < 0)
                throw new ArgumentOutOfRangeException(nameof(input));

            return input;
        }

        [Pure]
        public static Coordinate Parse(string value)
        {
            Match match = ParseRegex.Match(value);

            if (!match.Success)
                throw new FormatException("Invalid coordinate format!");

            if (match.Groups[1].Success)
            {
                return Parse(match.Groups[1].Value, match.Groups[2].Value);
            }
            else
            {
                return Parse(match.Groups[4].Value, match.Groups[3].Value);
            }
        }

        [Pure]
        public static Coordinate Parse(string xStr, string yStr)
        {
            int x = int.Parse(xStr);
            int y = VerticalLetters.IndexOf(yStr, StringComparison.InvariantCultureIgnoreCase);

            if (x < 1 || x > 10)
            {
                throw new ArgumentOutOfRangeException(nameof(xStr), "X must be between 1 and 10!");
            }

            if (y == -1)
            {
                throw new ArgumentOutOfRangeException(nameof(yStr), "Y must be between A and J!");
            }

            return new Coordinate
            {
                X = x - 1,
                Y = y
            };
        }

        public override string ToString()
        {
            return $"{Vertical}{Horizontal}";
        }

        [Pure]
        public static implicit operator Coordinate((int x, int y) coordinate)
        {
            return new Coordinate(coordinate.x, coordinate.y);
        }

        [Pure]
        public static implicit operator (int x, int y)(Coordinate coordinate)
        {
            return (coordinate.X, coordinate.Y);
        }

        [Pure]
        public void Deconstruct(out int x, out int y)
        {
            x = X;
            y = Y;
        }
    }
}