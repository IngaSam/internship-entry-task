using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using TicTacToe.Data;
using TicTacToe.Models;
using TicTacToe.Repositories;
using TicTacToe.Services;

namespace TicTacToe.Tests.UnitTests
{
    public class GameServiceTests: IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
            .WithDatabase("test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        private AppDbContext _dbContext;
        private GameService _service ;

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
            await _postgres.DisposeAsync();
        }

        [Fact]
        public async Task CreateGame_SetsCorrectBoardSize()
        {
            //Arrange
            const int size = 5;

            //Act
            var game = await _service.CreateGame(size);

            //Assert
            Assert.Equal(size, game.Board.GetLength(0));
            Assert.Equal(size, game.Board.GetLength(1));

            var dbGame = await _dbContext.Games.FindAsync(game.Id);
            Assert.NotNull(dbGame);
        }

        [Fact]
        public async Task MakeMove_AlternatesPlayers()
        {
            
            //Arrange
            var game = await _service.CreateGame(3);
            //Act
            var move1 = await _service.MakeMove(game.Id, new MoveRequest
            {
                Player = "X", Row =0, Col =0
            });
            Assert.Equal("X", move1.Board[0] [0]);


            var move2 = await _service.MakeMove(game.Id, new MoveRequest
            {
                Player ="O", Row = 1, Col = 1
            });            
            Assert.Equal("O", move2.Board[1] [1]);

            var updatedGame = await _dbContext.Games.FindAsync(game.Id);
            Assert.Equal("O", updatedGame.Board[1][1]);

        }
        [Fact]
        public async Task  MakeMove_ThrowsWhenCellOccupied()
        {
            //Arrange
            //var service = new GameService();
            var game = await  _service.CreateGame(3);
            await _service.MakeMove(game.Id, new MoveRequest
            {
                Player = "X",
                Row = 0,
                Col = 0
            });

            //Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.MakeMove(game.Id, new MoveRequest
            {
                Player = "O",
                Row = 0,
                Col = 0
            }));

        }
        [Fact]
        public async Task GameState_PersistsAfterRestart()
        {
            //Arrange
            var game = await _service.CreateGame(3);
            await _service.MakeMove(game.Id, new MoveRequest
            {
                Player = "X",
                Row = 0,
                Col = 0
            });

            //Simulate  restart
            var newDbContext = new AppDbContext(
                new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(_postgres.GetConnectionString())
                .Options);

            var newService = new GameService(new GameRepository(newDbContext));

            //Act
            var persistedGame = await newDbContext.Games.FindAsync(game.Id);

            //Assert
            Assert.Equal("X", persistedGame.Board[0][0]);
        }
    }
}
