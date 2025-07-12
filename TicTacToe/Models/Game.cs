namespace TicTacToe.Models
{
    public class Game
    {
        public Guid Id { get; set; }
        public string[,] Board { get; set; }
        public string CurrentPlayer { get; set; }
        public string? Winner { get; set; }
        public int MoveCount { get; set; }
    }
}
