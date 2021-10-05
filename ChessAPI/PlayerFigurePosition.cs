using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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
        public List<Vector2> LegalMoves => _game.LegalMovesRel;
        private bool _alive = true;

        public bool CanBeat(PlayerFigurePosition? target) => PlayerFigure != null && PlayerFigure.Player != target?.PlayerFigure?.Player;

        public bool MoveTo([Range(0, 7)] int x, [Range(0, 7)] int y)
        {
            var targetPos = _game[x, y];
            if (targetPos!.PlayerFigure != null && !CanBeat(targetPos))
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
            LegalMoves.Clear();
            switch (Figure)
            {
                case ChessAPI.Figure.Grunt:
                    LegalMoves.Add(new Vector2(1, 0));
                    // first move can be 2 tiles
                    if ((int) Position.X == (Player == ChessAPI.Player.PlayerOne ? 6 : 1))
                        LegalMoves.Add(new Vector2(2, 0));
                    _game.Repetition = LegalMoveRepetition.None;
                    break;
                case ChessAPI.Figure.Tower:
                    for (int x = 1; x < 8; x++)
                        LegalMoves.Add(new Vector2(x, 0));
                    _game.Repetition = LegalMoveRepetition.Radial;
                    break;
                case ChessAPI.Figure.Knight:
                    LegalMoves.Add(new Vector2(1,2));
                    _game.Repetition = LegalMoveRepetition.Radial;
                    break;
                case ChessAPI.Figure.Rogue:
                    for (int i = 1; i < 8; i++)
                        LegalMoves.Add(new Vector2(i, i));
                    _game.Repetition = LegalMoveRepetition.Quadlateral;
                    break;
                case ChessAPI.Figure.Queen:
                    for (int x = 1; x < 8; x++)
                        LegalMoves.Add(new Vector2(x, 0));
                    for (int i = 1; i < 8; i++)
                        LegalMoves.Add(new Vector2(i, i));
                    _game.Repetition = LegalMoveRepetition.Radial;
                    break;
                case ChessAPI.Figure.King:
                    LegalMoves.Add(new Vector2(1, 0));
                    LegalMoves.Add(new Vector2(1, 1));
                    _game.Repetition = LegalMoveRepetition.Radial;
                    break;
                case null:
                    _game.Repetition = LegalMoveRepetition.None;
                    throw new ArgumentOutOfRangeException();
                default:
                    _game.Repetition = LegalMoveRepetition.None;
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}