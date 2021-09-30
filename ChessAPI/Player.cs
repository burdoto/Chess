﻿namespace ChessAPI
{
    public enum Player
    {
        PlayerOne,
        PlayerTwo
    }

    public static class PlayerExtensions
    {
        public static Player Opposing(this Player it) => it == Player.PlayerOne ? Player.PlayerTwo : Player.PlayerOne;
    }
}