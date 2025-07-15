using System.Data;
using TicTacToe.Models;
using TicTacToe.Repositories;

namespace TicTacToe.Services
{
   
    public class GameService
    {
        private readonly GameRepository _repository;
        private readonly Random _random = new();

        public GameService(GameRepository repository)
        {
            _repository = repository;
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
                game.CurrentPlayer = game.CurrentPlayer == "X" ? "O" : "X";
            }
        }


        //private string? CheckWinner(string[][] board)
        //{
        //    int size = board.Length;
        //    // Проверка строк и столбцов
        //    for (int i = 0; i < size; i++)
        //    {
        //        if (CheckLine(board, i, 0, 0, 1)) return board[i][0];
        //        if (CheckLine(board, 0, i, 1, 0)) return board[0][i];
        //    }
        //    // Проверка диагоналей
        //    if (CheckLine(board, 0, 0, 1, 1)) return board[0][0];
        //    if (CheckLine(board, 0, size - 1, 1, -1)) return board[0][size - 1];
        //    // Проверка ничьи 
        //    return IsBoardFull(board) ? "Draw" : null;
        //}
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

        private bool CheckLine(string[][] board, int startX, int startY, int dx, int dy)
        {
            string first = board[startX][startY];
            if (string.IsNullOrEmpty(first)) return false;
            int size = board.Length;
            for (int i = 1; i < size; i++)
            {
                if (board[startX + i * dx][startY + i * dy] != first)
                    return false;
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
