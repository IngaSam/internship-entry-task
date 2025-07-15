using Microsoft.EntityFrameworkCore;
using TicTacToe.Data;
using TicTacToe.Models;

namespace TicTacToe.Repositories
{
    public class GameRepository
    {
        private readonly AppDbContext _context;

        public GameRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<Game> CreateGameAsync(Game game)
        {
         
            _context.Games.Add(game);
            await _context.SaveChangesAsync();
            return game;
        }
        public async Task<Game?> GetGameAsync(Guid id)
        {
            return await _context.Games.FindAsync(id);
        }
        public async Task<Game> UpdateGameAsync(Game game)
        {
            _context.Games.Update(game);
            await _context.SaveChangesAsync();
            return game;
        }
    }
}
