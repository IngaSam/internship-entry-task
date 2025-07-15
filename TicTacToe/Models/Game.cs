namespace TicTacToe.Models
{
    public class Game
    {
        public Guid Id { get; set; }= Guid.NewGuid();
        public string[][] Board { get; set; }
        public string CurrentPlayer { get; set; } = "X"; //Значения по умолччанию
        public string? Winner { get; set; } 
        public int MoveCount { get; set; }
    }
}
