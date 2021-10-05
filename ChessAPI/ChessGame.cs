using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Numerics;

namespace ChessAPI
{
    public enum LegalMoveMirroring
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
            get => x < 0 || y < 0 || x > 7 || y > 7 ? null : new PlayerFigurePosition(this, Board[x, y], x, y);
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

        #region Legal Move Evaluation
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
                 *              >[Mirroring == LegalMoveMirroring.Quadlateral
                 *                      ? MirrorQuadlateral
                 *                      : Mirroring == LegalMoveMirroring.Radial
                 *                              ? MirrorRadial
                 *                              : this]>
                 *              >[CalcRelative]>);
                 */

                var yield = LegalMovesRel.Select(PlayerMirror);
                if (Mirroring == LegalMoveMirroring.Quadlateral)
                    yield = yield.SelectMany(MirrorQuadlateral);
                else if (Mirroring == LegalMoveMirroring.Radial)
                    yield = yield.SelectMany(MirrorRadial);
                if (Repetition)
                    yield = yield.SelectMany(ApplyRepetition);
                return yield.Select(CalcRelative)
                    .Where(CheckLegalMoveValid);
            }
        }

        private IEnumerable<Vector2> ApplyRepetition(Vector2 rel)
        {
            var yield = new List<Vector2>();
            var off = rel;
            Vector2 abs;
            PlayerFigurePosition? pos;
            do
            {
                yield.Add(off);
                abs = CalcRelative(off += rel);
                pos = this[abs];
            }
            while (pos != null && pos.PlayerFigure?.Player == null);
            return yield;
        }

        private bool CheckLegalMoveValid(Vector2 abs)
        {
            var it = ActiveFigure;
            var target = this[abs];
            return it?.CanBeat(target) ?? false;
        }

        private Vector2 CalcRelative(Vector2 rel) => rel + (ActivePosition ?? Vector2.Zero);

        private Vector2 PlayerMirror(Vector2 rel) => ActivePlayer == Player.PlayerOne ? -rel : rel;

        private static readonly Vector2 ab = new Vector2(1, -1);

        private IEnumerable<Vector2> MirrorQuadlateral(Vector2 rel) => new[] { rel * Vector2.One, rel * ab, rel * -ab, rel * -Vector2.One };


        private IEnumerable<Vector2> MirrorRadial(Vector2 rel) => new[]
        {
            rel * Vector2.One, rel * ab, rel * -ab, rel * -Vector2.One,
            rel = new Vector2(rel.Y, rel.X) * Vector2.One, rel * ab, rel * -ab, rel * -Vector2.One
        };

        public LegalMoveMirroring Mirroring = LegalMoveMirroring.None;
        public bool Repetition = false;
        #endregion

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