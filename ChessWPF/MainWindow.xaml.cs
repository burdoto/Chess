using System;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ChessAPI;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Drawing.Color;
using Image = System.Windows.Controls.Image;

namespace ChessWPF
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static readonly BitmapImage[,] FigureImages = new BitmapImage[
            typeof(Player).GetEnumValues().Length,
            typeof(Figure).GetEnumValues().Length];

        public ChessGame Game = new();

        static MainWindow()
        {
            // load images
            var basePath = Directory.GetCurrentDirectory();
            foreach (var obj1 in typeof(Player).GetEnumValues())
                if (obj1 is Player plr)
                    foreach (var obj2 in typeof(Figure).GetEnumValues())
                        if (obj2 is Figure fig)
                        {
                            var path = ChessGame.GetImagePath(basePath, plr, fig);
                            var bmp = new BitmapImage(new Uri(path.FullName));
                            //if (plr == Player.PlayerOne) bmp = InvertBitmap(bmp); // inverse bmp
                            FigureImages[(int)plr, (int)fig] = bmp;
                        }
        }

        public MainWindow()
        {
            DataContext = this;
            InitializeComponent();

            Game.BoardUpdated += UpdateUI;
            Game.GameFinished += ResolveGame;
            Game.Start();
        }

        public Button? this[[Range(0, 7)] int x, [Range(0, 7)] int y] => Board.Children.Cast<Button>()
            .First(it => Grid.GetRow(it) == x && Grid.GetColumn(it) == y);

        private void ResolveGame(Player winner)
        {
            Game.ResetGame();
        }

        private void UpdateUI()
        {
            for (var x = 0; x < 8; x++)
            for (var y = 0; y < 8; y++)
            {
                var vec = new Vector2(x, y);
                var fig = Game.Board[x, y];
                var box = this[x, y];
                var legal = Game.LegalMoves.Contains(vec);

                if (box != null && fig != null)
                {
                    // todo Emplace actual images
                    box.Content = new Image { Width = 70, Source = FigureImages[(int)fig.Player, (int)fig.Figure] };
                    if (Game.ActivePosition?.X == x && Game.ActivePosition?.Y == y)
                        box.Background = Brushes.Blue;
                    else box.Background = (x + y) % 2 == 0 ? Brushes.SaddleBrown : Brushes.SandyBrown;
                    if (legal)
                        box.Background = Brushes.Green;
                    //box.Content = FigureImages[(int)fig.Player, (int)fig.Figure];
                    /* box string content
                    box.Content = "";
                    if (fig?.Player == Game.ActivePlayer)
                        box.Content += fig.Player + "\n";
                    box.Content += fig?.Figure.ToString() ?? "";
                    if (Game.ActivePosition == vec)
                        box.Content += '\n' + "SELECTED";
                    if (legal)
                        box.Content += '\n' + "LEGAL";
                        */
                }
                else if (box != null && legal)
                {
                    box.Background = Brushes.Green;
                }
                else if (box != null && fig == null)
                {
                    box.Background = (x + y) % 2 == 0 ? Brushes.SaddleBrown : Brushes.SandyBrown;
                    box.Content = "";
                }
            }

            if (Game.KingInDanger != null)
            {
                this[(int)Game.KingInDanger.Value.X, (int)Game.KingInDanger.Value.Y]!.Background = Brushes.Red;
            }

            DisplayPlayer.Text = "Active Player: " + Game.ActivePlayer;
            DisplaySelected.Text = "Selected Position: " + Game.ActivePosition;
        }

        private void ButtonClickHandler(object sender, RoutedEventArgs e)
        {
            if (sender is UIElement uie)
            {
                var x = Grid.GetRow(uie);
                var y = Grid.GetColumn(uie);
                Game.UseField(x, y);

                var pos = Game[x,y];
                if (pos?.Figure != null)
                {
                    pos.SetRelLegalMoves();
                    Game.KingInDanger = Game.LegalMoves
                        .Select(pos => Game[pos])
                        .FirstOrDefault(it => it?.Figure == Figure.King)?
                        .Position;
                }
            }
        }

        private void ResetSelection(object sender, RoutedEventArgs e)
        {
            Game.ResetSelection();
        }

        #region Bitmap Inversion

        private static BitmapImage InvertBitmap(BitmapImage bmp)
        {
            var pic = BitmapImage2Bitmap(bmp);
            for (var y = 0; y <= pic.Height - 1; y++)
            for (var x = 0; x <= pic.Width - 1; x++)
            {
                var inv = pic.GetPixel(x, y);
                inv = Color.FromArgb(255, 255 - inv.R, 255 - inv.G, 255 - inv.B);
                pic.SetPixel(x, y, inv);
            }

            return Bitmap2BitmapImage(pic);
        }

        private static Bitmap BitmapImage2Bitmap(BitmapImage bitmapImage)
        {
            // BitmapImage bitmapImage = new BitmapImage(new Uri("../Images/test.png", UriKind.Relative));

            using (MemoryStream outStream = new())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                Bitmap bitmap = new(outStream);

                return new Bitmap(bitmap);
            }
        }

        public static BitmapSource Convert(Bitmap bitmap)
        {
            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly, bitmap.PixelFormat);

            var bitmapSource = BitmapSource.Create(
                bitmapData.Width, bitmapData.Height,
                bitmap.HorizontalResolution, bitmap.VerticalResolution,
                PixelFormats.Bgr24, null,
                bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

            bitmap.UnlockBits(bitmapData);

            return bitmapSource;
        }

        private static BitmapImage Bitmap2BitmapImage(Bitmap bitmap)
        {
            BitmapSource bitmapSource = Convert(bitmap);

            JpegBitmapEncoder encoder = new();
            MemoryStream memoryStream = new();
            BitmapImage bImg = new();

            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
            encoder.Save(memoryStream);

            memoryStream.Position = 0;
            bImg.BeginInit();
            bImg.StreamSource = memoryStream;
            bImg.EndInit();

            memoryStream.Close();

            return bImg;
        }

        #endregion
    }
}