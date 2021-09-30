using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Numerics;

namespace ChessAPI
{
    public class PlayerFigurePosition : IPlayerFigure
    {
        public PlayerFigurePosition(ChessGame game, PlayerFigure? playerFigure, int x, int y) : this(game, playerFigure, new Vector2(x,y))
        {
        }

        public PlayerFigurePosition(ChessGame game, PlayerFigure? playerFigure, Vector2 position)
        {
            _game = game;
            PlayerFigure = playerFigure;
            Position = position;
        }

        private ChessGame _game;
        public PlayerFigure? PlayerFigure { get; }
        public Vector2 Position { get; internal set; }
        public readonly List<Vector2> LegalMoves = new List<Vector2>();
        private bool _alive = true;

        public bool CanBeat(PlayerFigurePosition target) => PlayerFigure != null && PlayerFigure.Player != target.PlayerFigure?.Player && LegalMoves.Contains(target.Position);

        public bool MoveTo([Range(0, 7)] int x, [Range(0, 7)] int y)
        {
            var targetPos = _game[x, y];
            if (targetPos.PlayerFigure != null && !CanBeat(targetPos))
                return false;
            _game[targetPos.Position] = this;
            return true;
        }

        bool IPlayerFigure.Alive
        {
            get => _alive;
            set => _alive = value;
        }

        public Player? Player => PlayerFigure?.Player;
        public Figure? Figure => PlayerFigure?.Figure;

        public void CalculateLegalMoves()
        {
            // todo
        }
    }
}