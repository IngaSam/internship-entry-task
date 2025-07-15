namespace TicTacToe.Services.Providers
{
    public class DefaultRandomProvider : IRandomProvider
    {
        private readonly Random _random = new();
        public int Next(int maxValue) => _random.Next(maxValue);
    }
}
