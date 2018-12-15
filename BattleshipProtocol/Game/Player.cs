using System;
using System.Net;
using JetBrains.Annotations;

namespace BattleshipProtocol.Game
{
    public class Player
    {
        [CanBeNull] private string _name;

        [NotNull]
        public Board Board { get; set; }

        [CanBeNull]
        public string Name
        {
            get => _name;
            set { _name = value; OnNameChanged(); }
        }

        public bool IsLocal { get; }

        [NotNull]
        public EndPoint EndPoint { get; }

        public event EventHandler NameChanged;

        public Player(bool isLocal, EndPoint endPoint)
        {
            IsLocal = isLocal;
            EndPoint = endPoint;
            Board = new Board();
        }

        protected virtual void OnNameChanged()
        {
            NameChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}