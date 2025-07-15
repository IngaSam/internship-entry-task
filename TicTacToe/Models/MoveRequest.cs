namespace TicTacToe.Models
{
    public class MoveRequest
    {
        public string Player { get; set; } = "X"; //Значения по умолчанию
        public int Row { get; set; }
        public int Col { get; set; }
    }
}
