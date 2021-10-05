using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ChessAPI;

namespace ChessWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ChessGame Game = new ChessGame();

        public MainWindow()
        {
            DataContext = this;
            InitializeComponent();

            Game.BoardUpdated += UpdateUI;
            Game.Start();
        }

        public Button? this[[Range(0, 7)] int x, [Range(0, 7)] int y] => Board.Children.Cast<Button>()
                    .First(it => Grid.GetRow(it) == x && Grid.GetColumn(it) == y);
        private void UpdateUI()
        {
            for (int x = 0; x < 8; x++)
            for (int y = 0; y < 8; y++)
            {
                var vec = new Vector2(x, y);
                var fig = Game.Board[x, y];
                var box = this[x, y];
                var legal = Game.LegalMoves.Contains(vec);

                if (box != null && (fig != null || legal))
                {
                    box.Content = "";
                    if (fig?.Player == Game.ActivePlayer)
                        box.Content += fig.Player + "\n";
                    box.Content += fig?.Figure.ToString() ?? "";
                    if (Game.ActivePosition == vec)
                        box.Content += '\n' + "SELECTED";
                    if (legal)
                        box.Content += '\n' + "LEGAL";
                }
                else if (box != null) box.Content = "";
            }

            DisplayPlayer.Text = "This player's turn:" + Game.ActivePlayer;
            DisplaySelected.Text = "Selected Position: " + Game.ActivePosition;
        }

        private void ButtonClickHandler(object sender, RoutedEventArgs e)
        {
            if (sender is UIElement uie)
               Game.UseField(Grid.GetRow(uie), Grid.GetColumn(uie));
        }

        private void ResetSelection(object sender, RoutedEventArgs e)
        {
            Game.ResetSelection();
        }
    }
}
