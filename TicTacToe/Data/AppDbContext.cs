using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using TicTacToe.Models;

namespace TicTacToe.Data
{
    public class AppDbContext: DbContext
    { 
        public DbSet<Game> Games { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Game>()
                .Property(g => g.Board)
                .HasConversion(
                v => JsonConvert.SerializeObject(v),
                v => JsonConvert.DeserializeObject<string[][]>(v));
        }       
        
    }
}
