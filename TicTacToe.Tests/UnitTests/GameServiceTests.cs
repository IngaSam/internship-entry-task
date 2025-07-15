using Microsoft.EntityFrameworkCore;
using Moq;
using Testcontainers.PostgreSql;
using TicTacToe.Data;
using TicTacToe.Models;
using TicTacToe.Repositories;
using TicTacToe.Services;

namespace TicTacToe.Tests.UnitTests
{
    public class GameServiceTests : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16")
            .WithDatabase("test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        private AppDbContext _dbContext;
        private GameService _service;

        public async Task InitializeAsync()
        {
            await _postgres.StartAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(_postgres.GetConnectionString())
                .Options;
            _dbContext = new AppDbContext(options);
            await _dbContext.Database.MigrateAsync();

            var repository = new GameRepository(_dbContext);
            _service = new GameService(repository);
        }

        public async Task DisposeAsync()
        {
            await _dbContext.DisposeAsync();
            await _postgres.DisposeAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task CreateGame_SetsCorrectBoardSize()
        {
            // Arrange
            const int size = 5;

            // Act
            var game = await _service.CreateGame(size);

            // Assert
            Assert.Equal(size, game.Board.Length);
            Assert.Equal(size, game.Board[0].Length);
            Assert.Equal("X", game.CurrentPlayer);

            var dbGame = await _dbContext.Games.FindAsync(game.Id);
            Assert.NotNull(dbGame);
            Assert.Equal(size, dbGame.Board.Length);
        }

        [Fact]
        public async Task MakeMove_AlternatesPlayers()
        {
            // Arrange
            var game = await _service.CreateGame(3);

            // Act & Assert - First move (X)
            var move1 = await _service.MakeMove(game.Id, new MoveRequest
            {
                Player = "X",
                Row = 0,
                Col = 0
            });
            Assert.Equal("X", move1.Board[0][0]);
            Assert.Equal("O", move1.CurrentPlayer);

            // Second move (O)
            var move2 = await _service.MakeMove(game.Id, new MoveRequest
            {
                Player = "O",
                Row = 1,
                Col = 1
            });
            Assert.Equal("O", move2.Board[1][1]);
            Assert.Equal("X", move2.CurrentPlayer);

            // Verify DB state
            var dbGame = await _dbContext.Games.FindAsync(game.Id);
            Assert.Equal("O", dbGame.Board[1][1]);
            Assert.Equal("X", dbGame.CurrentPlayer);
        }

        [Fact]
        public async Task MakeMove_ThrowsWhenCellOccupied()
        {
            // Arrange
            var game = await _service.CreateGame(3);
            await _service.MakeMove(game.Id, new MoveRequest
            {
                Player = "X",
                Row = 0,
                Col = 0
            });

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.MakeMove(game.Id, new MoveRequest
                {
                    Player = "O",
                    Row = 0,
                    Col = 0
                }));

            Assert.Contains("already occupied", exception.Message);
        }

        [Fact]
        public async Task MakeMove_ThrowsWhenWrongPlayer()
        {
            // Arrange
            var game = await _service.CreateGame(3);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.MakeMove(game.Id, new MoveRequest
                {
                    Player = "O", // Should be X first
                    Row = 0,
                    Col = 0
                }));

            Assert.Contains("Expected: X", exception.Message);
        }

        [Fact]
        public async Task MakeMove_AppliesSpecialRule()
        {
            // Arrange
            var game = await _service.CreateGame(3);
            var mockRandom = new Mock<IRandomProvider>();
            mockRandom.Setup(r => r.Next(10)).Returns(0); // Для теста специального правила
            var serviceWithMock = new GameService(new GameRepository(_dbContext), mockRandom.Object);

            await _service.MakeMove(game.Id, new MoveRequest { Player = "X", Row = 0, Col = 0 });
            await _service.MakeMove(game.Id, new MoveRequest { Player = "O", Row = 1, Col = 1 });

            // Act - Third move should trigger special rule
            var result = await serviceWithMock.MakeMove(game.Id, new MoveRequest { Player = "X", Row = 2, Col = 2 });

            // Assert
            Assert.Equal("O", result.Board[2][2]);
            Assert.Equal("X", result.CurrentPlayer);
            Assert.Equal(3, result.MoveCount);
            Assert.Null(result.Winner);
        }

        [Fact]
        public async Task GameState_PersistsAfterRestart()
        {
            // Arrange
            var game = await _service.CreateGame(3);
            await _service.MakeMove(game.Id, new MoveRequest
            {
                Player = "X",
                Row = 0,
                Col = 0
            });

            // Simulate restart
            var newDbContext = new AppDbContext(
                new DbContextOptionsBuilder<AppDbContext>()
                    .UseNpgsql(_postgres.GetConnectionString())
                    .Options);

            var newService = new GameService(new GameRepository(newDbContext));

            // Act
            var persistedGame = await newService.GetGameAsync(game.Id);

            // Assert
            Assert.Equal("X", persistedGame.Board[0][0]);
            Assert.Equal("O", persistedGame.CurrentPlayer);
        }

        [Fact]
        public async Task CheckWinner_DetectsWinCorrectly()
        {
            // Arrange
            var game = await _service.CreateGame(3);
            Console.WriteLine($"Initial player: {game.CurrentPlayer}");

            // Делаем ходы строго по очереди
            // 1. X (ожидаемый текущий игрок после создания игры)
            game = await _service.MakeMove(game.Id, new MoveRequest
            {
                Player = game.CurrentPlayer,  // Используем текущего игрока из состояния игры
                Row = 0,
                Col = 0
            });

            // 2. O (должен быть следующим)
            game = await _service.MakeMove(game.Id, new MoveRequest
            {
                Player = game.CurrentPlayer,  // Берём текущего игрока из обновлённого состояния
                Row = 1,
                Col = 0
            });

            // 3. X
            game = await _service.MakeMove(game.Id, new MoveRequest
            {
                Player = game.CurrentPlayer,
                Row = 0,
                Col = 1
            });

            // 4. O
            game = await _service.MakeMove(game.Id, new MoveRequest
            {
                Player = game.CurrentPlayer,
                Row = 1,
                Col = 1
            });

            // 5. X - выигрышный ход
            var result = await _service.MakeMove(game.Id, new MoveRequest
            {
                Player = game.CurrentPlayer,
                Row = 0,
                Col = 2
            });

            // Assert
            Assert.Equal("X", result.Winner);
            Assert.Equal(5, result.MoveCount);
        }

    }
}