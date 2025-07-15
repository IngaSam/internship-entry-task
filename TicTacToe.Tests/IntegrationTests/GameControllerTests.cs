using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using TicTacToe.Controllers;
using TicTacToe.Data;
using TicTacToe.Models;
using TicTacToe.Repositories;
using TicTacToe.Services;

namespace TicTacToe.Tests.IntegrationTests
{
    public class GameControllerTests : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16")
            .WithDatabase("test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        private AppDbContext _dbContext;
        private GameService _service;
        private GamesController _controller;

        public async Task InitializeAsync()
        {
            await _postgresContainer.StartAsync();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(_postgresContainer.GetConnectionString())
                .Options;

            _dbContext = new AppDbContext(options);
            await _dbContext.Database.MigrateAsync();

            var repository = new GameRepository(_dbContext);
            _service = new GameService(repository);
            _controller = new GamesController(_service);
        }

        public async Task DisposeAsync()
        {
            await _dbContext.DisposeAsync();
            await _postgresContainer.DisposeAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task CreateGame_ReturnsValidGame()
        {
            // Act
            var result = await _controller.CreateGame();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var game = Assert.IsType<Game>(okResult.Value);

            Assert.NotEqual(Guid.Empty, game.Id);
            Assert.Equal(3, game.Board.Length); // Default size from env

            // Verify DB
            var dbGame = await _dbContext.Games.FindAsync(game.Id);
            Assert.NotNull(dbGame);
        }

        [Fact]
        public async Task GetGame_ReturnsNotFoundForInvalidId()
        {
            // Act
            var result = await _controller.GetGame(Guid.NewGuid());

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task MakeMove_ReturnsBadRequestForInvalidMove()
        {
            // Arrange
            var createResult = await _controller.CreateGame();
            var okResult = Assert.IsType<OkObjectResult>(createResult);
            var game = Assert.IsType<Game>(okResult.Value);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act 1 - Test null move
            var nullResult = await _controller.MakeMove(game.Id, null);

            // Assert 1
            var nullBadRequest = Assert.IsType<BadRequestObjectResult>(nullResult);
            var nullProblem = Assert.IsType<ProblemDetails>(nullBadRequest.Value);
            Assert.Equal("Invalid request", nullProblem.Title);
            Assert.Equal(StatusCodes.Status400BadRequest, nullProblem.Status);

            // Act 2 - Test wrong player
            var wrongPlayerResult = await _controller.MakeMove(game.Id, new MoveRequest
            {
                Player = "O", // Should be X first
                Row = 0,
                Col = 0
            });

            // Assert 2
            var badRequest = Assert.IsType<BadRequestObjectResult>(wrongPlayerResult);
            var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
            Assert.Contains("Expected: X", problem.Detail);
            Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        }

        [Fact]
        public async Task FullGameFlow_WorksCorrectly()
        {
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Create game
            var createResult = await _controller.CreateGame();
            var game = (Game)((OkObjectResult)createResult).Value;

            // Make moves
            await _controller.MakeMove(game.Id, new MoveRequest { Player = "X", Row = 0, Col = 0 });
            await _controller.MakeMove(game.Id, new MoveRequest { Player = "O", Row = 1, Col = 0 });
            await _controller.MakeMove(game.Id, new MoveRequest { Player = "X", Row = 0, Col = 1 });
            await _controller.MakeMove(game.Id, new MoveRequest { Player = "O", Row = 1, Col = 1 });
            var finalResult = await _controller.MakeMove(game.Id, new MoveRequest { Player = "X", Row = 0, Col = 2 });

            // Check winner
            var okResult = Assert.IsType<OkObjectResult>(finalResult);
            var finalGame = Assert.IsType<Game>(okResult.Value);
            Assert.Equal("X", finalGame.Winner);
        }
    }
}