namespace ChessAPI
{
    public interface IPlayerFigure
    {
        bool Alive { get; internal set; }
        Player? Player { get; }
        Figure? Figure { get; }
    }

    public class PlayerFigure : IPlayerFigure
    {
        private bool _alive = true;

        public PlayerFigure(Player player, Figure figure)
        {
            Player = player;
            Figure = figure;
        }

        internal PlayerFigure()
        {
        }

        bool IPlayerFigure.Alive
        {
            get => _alive;
            set => _alive = value;
        }

        public Player? Player { get; }
        public Figure? Figure { get; }
    }
}