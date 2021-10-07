using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Numerics;
//using System.Windows.Media.Imaging;

namespace ChessAPI
{
    public enum LegalMoveMirroring
    {
        None,
        Diagonal,
        Radial
    }

    public class ChessGame
    {
        public event Action BoardUpdated;
        public event Action<Player> GameFinished;
        public PlayerFigure?[,] Board { get; private set; } = ResetBoard();

        private static PlayerFigure?[,] ResetBoard() => new[,] 
        {
            { // p2 royal row
                new PlayerFigure(Player.PlayerTwo, Figure.Tower), new PlayerFigure(Player.PlayerTwo, Figure.Knight),
                new PlayerFigure(Player.PlayerTwo, Figure.Rogue), new PlayerFigure(Player.PlayerTwo, Figure.Queen),
                new PlayerFigure(Player.PlayerTwo, Figure.King), new PlayerFigure(Player.PlayerTwo, Figure.Rogue),
                new PlayerFigure(Player.PlayerTwo, Figure.Knight), new PlayerFigure(Player.PlayerTwo, Figure.Tower)
            },
            { // p2 peasant row
                new PlayerFigure(Player.PlayerTwo, Figure.Grunt), new PlayerFigure(Player.PlayerTwo, Figure.Grunt),
                new PlayerFigure(Player.PlayerTwo, Figure.Grunt), new PlayerFigure(Player.PlayerTwo, Figure.Grunt),
                new PlayerFigure(Player.PlayerTwo, Figure.Grunt), new PlayerFigure(Player.PlayerTwo, Figure.Grunt),
                new PlayerFigure(Player.PlayerTwo, Figure.Grunt), new PlayerFigure(Player.PlayerTwo, Figure.Grunt)
            },
            { null, null, null, null, null, null, null, null },
            { null, null, null, null, null, null, null, null },
            { null, null, null, null, null, null, null, null },
            { null, null, null, null, null, null, null, null },
            { // p1 soldier row
                new PlayerFigure(Player.PlayerOne, Figure.Grunt), new PlayerFigure(Player.PlayerOne, Figure.Grunt),
                new PlayerFigure(Player.PlayerOne, Figure.Grunt), new PlayerFigure(Player.PlayerOne, Figure.Grunt),
                new PlayerFigure(Player.PlayerOne, Figure.Grunt), new PlayerFigure(Player.PlayerOne, Figure.Grunt),
                new PlayerFigure(Player.PlayerOne, Figure.Grunt), new PlayerFigure(Player.PlayerOne, Figure.Grunt)
            },
            { // p1 imperial row
                new PlayerFigure(Player.PlayerOne, Figure.Tower), new PlayerFigure(Player.PlayerOne, Figure.Knight),
                new PlayerFigure(Player.PlayerOne, Figure.Rogue), new PlayerFigure(Player.PlayerOne, Figure.Queen),
                new PlayerFigure(Player.PlayerOne, Figure.King), new PlayerFigure(Player.PlayerOne, Figure.Rogue),
                new PlayerFigure(Player.PlayerOne, Figure.Knight), new PlayerFigure(Player.PlayerOne, Figure.Tower)
            }
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
                 *              >[Mirroring == LegalMoveMirroring.Diagonal
                 *                      ? MirrorDiagonal
                 *                      : Mirroring == LegalMoveMirroring.Radial
                 *                              ? MirrorRadial
                 *                              : this]>
                 *              >[CalcRelative]>);
                 */

                var yield = LegalMovesRel.Select(PlayerMirror);
                if (Mirroring == LegalMoveMirroring.Diagonal)
                    yield = yield.SelectMany(MirrorDiagonal);
                else if (Mirroring == LegalMoveMirroring.Radial)
                    yield = yield.SelectMany(MirrorRadial);
                if (Repetition)
                    yield = yield.SelectMany(ApplyRepetition);
                return yield.Select(CalcRelative)
                    .Where(CheckLegalMoveValid)
                    .Distinct();
            }
        }

        private IEnumerable<Vector2> ApplyRepetition(Vector2 rel)
        {
            var yield = new List<Vector2>();
            var off = rel;
            Vector2 abs = CalcRelative(off);
            PlayerFigurePosition? pos = this[abs];
            var opponent = ActivePlayer.Opposing();
            while (pos != null && (pos.Player == null || pos.Player == opponent))
            {
                yield.Add(off);
                if (pos.Player == opponent)
                    break;
                abs = CalcRelative(off += rel);
                pos = this[abs];
            }
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

        private Vector2[] MirrorDiagonal(Vector2 rel) => new[] { rel * Vector2.One, rel * ab, rel * -ab, rel * -Vector2.One };


        private Vector2[] MirrorRadial(Vector2 rel) => new[]
        {
            rel * Vector2.One, rel * ab, rel * -ab, rel * -Vector2.One,
            rel = new Vector2(rel.Y, rel.X) * Vector2.One, rel * ab, rel * -ab, rel * -Vector2.One
        };

        public LegalMoveMirroring Mirroring = LegalMoveMirroring.None;
        public bool Repetition = false;
        #endregion

        public void ResetGame()
        {
            Board = ResetBoard();
            ActivePlayer = Player.PlayerOne;
            ResetSelection();
        }

        public void ResetSelection()
        {
            ActivePosition = null;
            BoardUpdated();
        }

        public void UseField([Range(0, 7)] int x, [Range(0, 7)] int y)
        {
            var sel = new Vector2(x, y);
            if (ActivePosition == null && Board[x, y]?.Player == ActivePlayer) // select
                this[(Vector2)(ActivePosition = new Vector2(x, y))]!.SetRelLegalMoves();
            else if (ActivePosition != null && LegalMoves.Contains(sel)) // apply move
                if (this[(Vector2)ActivePosition]!.MoveTo(x, y))
                {
                    ActivePlayer = ActivePlayer.Opposing();
                    LegalMovesRel.Clear();
                    ActivePosition = null;
                }

            BoardUpdated();
            CheckWinConditions();
        }

        public IEnumerable<PlayerFigurePosition> All()
        {
            var yield = new List<PlayerFigurePosition>();
            PlayerFigurePosition? acc;
            for (int x = 0; x < 8; x++)
            for (int y = 0; y < 8; y++)
                if ((acc = this[x, y]) != null)
                    yield.Add(acc);
            return yield;
        }

        private void CheckWinConditions()
        {
            if (!All().Any(it => it.Figure == Figure.King && it.Player == Player.PlayerOne))
                GameFinished(Player.PlayerTwo);
            if (!All().Any(it => it.Figure == Figure.King && it.Player == Player.PlayerTwo))
                GameFinished(Player.PlayerOne);
        }

        public static FileInfo GetImagePath(string basePath, Player plr, Figure fig) =>
            new(Path.Combine(Path.Combine(basePath,"Assets/"), $"p{(plr == Player.PlayerOne ? '1' : '2')}-{fig}.bmp"));
    }
}