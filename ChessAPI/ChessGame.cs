using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Numerics;

namespace ChessAPI
{
    public enum LegalMoveRepetition
    {
        None,
        Quadlateral,
        Radial
    }

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

        public PlayerFigurePosition? this[Vector2 pos]
        {
            get => this[(int)pos.X, (int)pos.Y];
            set => this[(int)pos.X, (int)pos.Y] = value;
        }

        public PlayerFigurePosition? this[[Range(0, 7)] int x, [Range(0, 7)] int y]
        {
            get => new PlayerFigurePosition(this, Board[x, y], x, y);
            set
            {
                PlayerFigure? swap = Board[x, y];
                if (swap is IPlayerFigure ipf)
                    ipf.Alive = false;
                Board[x, y] = value?.PlayerFigure;
                if (value != null)
                {
                    this[value.Position] = null;
                    value.Position = new Vector2(x, y);
                }
            }
        }

        public Player ActivePlayer { get; private set; } = Player.PlayerOne;

        public Vector2? ActivePosition { get; internal set; }

        public PlayerFigurePosition? ActiveFigure => !ActivePosition.HasValue ? null : this[ActivePosition.Value]!;

        public List<Vector2> LegalMovesRel { get; } = new List<Vector2>();

        public IEnumerable<Vector2> LegalMoves
        {
            get
            {
                /*
                 * Equal KScr Code example:
                 *
                 * public iterable<Vector2> LegalMoves << (LegalMovesRel
                 *              >[PlayerMirror]>
                 *              >[Repetition == LegalMoveRepetition.Quadlateral
                 *                      ? MirrorQuadlateral
                 *                      : Repetition == LegalMoveRepetition.Radial
                 *                              ? MirrorRadial
                 *                              : this]>
                 *              >[CalcRelative]>);
                 */

                var yield = LegalMovesRel.Select(PlayerMirror);
                if (Repetition == LegalMoveRepetition.Quadlateral)
                    yield = yield.SelectMany(MirrorQuadlateral);
                else if (Repetition == LegalMoveRepetition.Radial)
                    yield = yield.SelectMany(MirrorRadial);
                return yield.Select(CalcRelative)
                    .Where(CheckLegalMoveValid);
            }
        }

        private bool CheckLegalMoveValid(Vector2 arg)
        {
            var it = ActiveFigure;
            var target = this[arg];
            return it?.CanBeat(target) ?? false;
        }

        private Vector2 CalcRelative(Vector2 arg) => arg + (ActivePosition ?? Vector2.Zero);

        private Vector2 PlayerMirror(Vector2 arg) => ActivePlayer == Player.PlayerOne ? -arg : arg;

        private static readonly Vector2 ab = new Vector2(1, -1);

        private IEnumerable<Vector2> MirrorQuadlateral(Vector2 arg) => new[] { arg * Vector2.One, arg * ab, arg * -ab, arg * -Vector2.One };


        private IEnumerable<Vector2> MirrorRadial(Vector2 arg) => new[]
        {
            arg * Vector2.One, arg * ab, arg * -ab, arg * -Vector2.One,
            arg = new Vector2(arg.Y, arg.X) * Vector2.One, arg * ab, arg * -ab, arg * -Vector2.One
        };

        public LegalMoveRepetition Repetition = LegalMoveRepetition.None;

        public void ResetSelection()
        {
            ActivePosition = null;
            BoardUpdated();
        }

        public void UseField([Range(0, 7)] int x, [Range(0, 7)] int y)
        {
            var sel = new Vector2(x, y);
            if (ActivePosition == null && Board[x, y]?.Player == ActivePlayer) // select
                this[(Vector2)(ActivePosition = new Vector2(x, y))]!.CalculateLegalMoves();
            else if (ActivePosition != null && LegalMoves.Contains(sel)) // apply move
                if (this[(Vector2)ActivePosition]!.MoveTo(x, y))
                {
                    ActivePlayer = ActivePlayer.Opposing();
                    LegalMovesRel.Clear();
                    ActivePosition = null;
                }

            BoardUpdated();
        }
    }
}