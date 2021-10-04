using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Numerics;

namespace ChessAPI
{
    public class ChessGame
    {
        public event Action BoardUpdated;
        public event Action<Player> GameFinished;
        public readonly PlayerFigure?[,] Board = {
            {new PlayerFigure(Player.PlayerTwo, Figure.Tower),new PlayerFigure(Player.PlayerTwo, Figure.Knight),new PlayerFigure(Player.PlayerTwo, Figure.Rogue),new PlayerFigure(Player.PlayerTwo, Figure.Queen),new PlayerFigure(Player.PlayerTwo, Figure.King),new PlayerFigure(Player.PlayerTwo, Figure.Rogue),new PlayerFigure(Player.PlayerTwo, Figure.Knight),new PlayerFigure(Player.PlayerTwo, Figure.Tower)},
            {new PlayerFigure(Player.PlayerTwo, Figure.Grunt),new PlayerFigure(Player.PlayerTwo, Figure.Grunt),new PlayerFigure(Player.PlayerTwo, Figure.Grunt),new PlayerFigure(Player.PlayerTwo, Figure.Grunt),new PlayerFigure(Player.PlayerTwo, Figure.Grunt),new PlayerFigure(Player.PlayerTwo, Figure.Grunt),new PlayerFigure(Player.PlayerTwo, Figure.Grunt),new PlayerFigure(Player.PlayerTwo, Figure.Grunt)},
            {null,null,null,null,null,null,null,null},
            {null,null,null,null,null,null,null,null},
            {null,null,null,null,null,null,null,null},
            {null,null,null,null,null,null,null,null},
            {new PlayerFigure(Player.PlayerOne, Figure.Grunt),new PlayerFigure(Player.PlayerOne, Figure.Grunt),new PlayerFigure(Player.PlayerOne, Figure.Grunt),new PlayerFigure(Player.PlayerOne, Figure.Grunt),new PlayerFigure(Player.PlayerOne, Figure.Grunt),new PlayerFigure(Player.PlayerOne, Figure.Grunt),new PlayerFigure(Player.PlayerOne, Figure.Grunt),new PlayerFigure(Player.PlayerOne, Figure.Grunt)},
            {new PlayerFigure(Player.PlayerOne, Figure.Tower),new PlayerFigure(Player.PlayerOne, Figure.Knight),new PlayerFigure(Player.PlayerOne, Figure.Rogue),new PlayerFigure(Player.PlayerOne, Figure.Queen),new PlayerFigure(Player.PlayerOne, Figure.King),new PlayerFigure(Player.PlayerOne, Figure.Rogue),new PlayerFigure(Player.PlayerOne, Figure.Knight),new PlayerFigure(Player.PlayerOne, Figure.Tower)}
        };

        public void Start() => BoardUpdated();

        public PlayerFigurePosition this[Vector2 pos]
        {
            get => this[(int)pos.X, (int)pos.Y];
            set => this[(int)pos.X, (int)pos.Y] = value;
        }

        public PlayerFigurePosition this[[Range(0, 7)] int x, [Range(0, 7)] int y]
        {
            get => new PlayerFigurePosition(this, Board[x, y], x, y);
            set
            {
                PlayerFigure? swap = Board[x, y];
                if (swap is IPlayerFigure ipf)
                    ipf.Alive = false;
                Board[x, y] = value.PlayerFigure;
                value.Position = new Vector2(x, y);
            }
        }

        public Player ActivePlayer { get; private set; } = Player.PlayerOne;

        public Vector2? ActivePosition { get; internal set; }

        public IEnumerable<Vector2>? LegalMoves
        {
            get
            {
                if (ActivePosition.HasValue)
                {
                    var position = this[ActivePosition.Value];
                    return position.LegalMoves;
                }

                return Array.Empty<Vector2>();
            }
        }

        public void UseField([Range(0, 7)] int x, [Range(0, 7)] int y)
        {
            if (ActivePosition == null && Board[x, y]?.Player == ActivePlayer) // select
                this[(Vector2)(ActivePosition = new Vector2(x, y))].CalculateLegalMoves();
            else if (ActivePosition != null && (LegalMoves?.Contains(ActivePosition ?? -Vector2.One) ?? false)) // apply move
#pragma warning disable 8629
                if (this[(Vector2)ActivePosition].MoveTo(x, y))
#pragma warning restore 8629
                    ActivePlayer = ActivePlayer.Opposing();
            BoardUpdated();
        }
    }
}