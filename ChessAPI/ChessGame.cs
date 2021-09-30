using System;

namespace ChessAPI
{
    public class ChessGame
    {
        public event Action BoardUpdated;

        public PlayerFigure[,] Board = new PlayerFigure[8,8];
    }
}