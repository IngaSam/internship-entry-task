using System.Data;
using TicTacToe.Models;
using TicTacToe.Repositories;
using TicTacToe.Services.Providers;

namespace TicTacToe.Services
{
   
    public class GameService
    {
        private readonly GameRepository _repository;
        private readonly IRandomProvider _random;

        public GameService(GameRepository repository, IRandomProvider? randomProvider = null)
        {
            _repository = repository;
            _random = randomProvider ?? new DefaultRandomProvider();
        }

        public async Task<Game> CreateGame(int size)
        {
            var game = new Game
            {
                Id = Guid.NewGuid(),
                Board = new string[size][], // Используем строковый массив    
                CurrentPlayer = "X",
                MoveCount = 0
            };

            for (int i = 0; i < size; i++)
            {
                game.Board[i] = new string[size]; // Инициализируем каждый ряд
            }

            return await _repository.CreateGameAsync(game);
        }

        public async Task<Game?> GetGameAsync(Guid id)
        {
            return await _repository.GetGameAsync(id);
        }

        public async Task<Game> MakeMove(Guid gameId, MoveRequest move)
        {
            if (move == null)
            {
                throw new ArgumentNullException(nameof(move), "Move request cannot be null");
            }

            var game = await _repository.GetGameAsync(gameId)
                ?? throw new KeyNotFoundException("Game not found");

            ValidateMove(game, move);

            ApplySpecialRule(game, ref move);

            UpdateGameState(game, move);

            // Проверка победы
            game.Winner = CheckWinner(game.Board);
            return await _repository.UpdateGameAsync(game);
        }

        private void ValidateMove(Game game, MoveRequest move)
        {

            //Проверка валидатности хода
            if (game.Board[move.Row] [move.Col] != null)
                throw new InvalidOperationException("Cell already occupied");

            if (game.Winner != null)
                throw new InvalidOperationException("Game is alredy finished");

            // отладочный вывод перед проверкой игрока
            Console.WriteLine($"Validating move: CurrentPlayer={game.CurrentPlayer}, Move.Player={move.Player}");

            if (move.Player != game.CurrentPlayer)
                throw new InvalidOperationException($"Not player's turn. Expected: {game.CurrentPlayer}");

            }

        private void ApplySpecialRule(Game game, ref MoveRequest move)
        {
            // Особое правило (10% на 3-й ход)
            if (game.MoveCount % 3 == 2 && _random.Next(10) == 0)
                {
                    move.Player = game.CurrentPlayer == "X" ? "O" : "X";
                }
        }

        private void UpdateGameState(Game game, MoveRequest move)
        {
            game.Board[move.Row][move.Col] = move.Player;
            game.MoveCount++;

            // Проверяем победителя перед сменой игрока
            game.Winner = CheckWinner(game.Board);

            // Меняем игрока только если игра продолжается
            if (game.Winner == null)
            {
                if (move.Player == game.CurrentPlayer)
                {
                    game.CurrentPlayer = game.CurrentPlayer == "X" ? "O" : "X";
                }
            }
        }
        private string? CheckWinner(string[][] board)
        {
            int size = board.Length;
            int winLength = int.Parse(Environment.GetEnvironmentVariable("WIN_LENGTH") ?? size.ToString());

            // Проверка по горизонтали, вертикали и диагоналям
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    if (!string.IsNullOrEmpty(board[i][j]))
                    {
                        // Горизонталь
                        if (CheckDirection(board, i, j, 0, 1, winLength)) return board[i][j];
                        // Вертикаль
                        if (CheckDirection(board, i, j, 1, 0, winLength)) return board[i][j];
                        // Диагональ вправо-вниз
                        if (CheckDirection(board, i, j, 1, 1, winLength)) return board[i][j];
                        // Диагональ влево-вниз
                        if (CheckDirection(board, i, j, 1, -1, winLength)) return board[i][j];
                    }
                }
            }

            return IsBoardFull(board) ? "Draw" : null;
        }

        private bool IsBoardFull(string[][] board)
        {
            int size = board.Length;
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    if (string.IsNullOrEmpty(board[i][j]))
                        return false;
                }
            }
            return true;
        }

        private bool CheckDirection(string[][] board, int startX, int startY, int dx, int dy, int winLength)
        {
            string symbol = board[startX][startY];
            int size = board.Length;

            for (int step = 1; step < winLength; step++)
            {
                int x = startX + step * dx;
                int y = startY + step * dy;

                if (x < 0 || x >= size || y < 0 || y >= size || board[x][y] != symbol)
                    return false;
            }

            return true;
        }
    }
}
