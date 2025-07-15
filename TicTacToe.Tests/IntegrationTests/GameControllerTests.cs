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
    public class GameControllerTests
    {
        private  GamesController _controller;
        private  GameService _service;
        private  AppDbContext _dbContext;
        private  PostgreSqlContainer _postgresContainer;

        public GameControllerTests()
        {

            // Настройка тестовой БД
            _postgresContainer = new PostgreSqlBuilder()
                .WithDatabase("test")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();

            // Инициализация будет в InitializeAsync
            _dbContext = null!;
            _service = null!;
            _controller = null!;

        }
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
            await _postgresContainer.DisposeAsync();
        }

        [Fact]
        public async Task CreateGame_ReturnsValidGame()
        {
            //Act
            var result = _controller.CreateGame();

            //Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var game = Assert.IsType<Game>(okResult.Value);
            Assert.NotEqual(Guid.Empty, game.Id);

            // Проверка сохранения в БД
            var dbGame = await _dbContext.Games.FindAsync(game.Id);
            Assert.NotNull(dbGame);
        }

    }
}
