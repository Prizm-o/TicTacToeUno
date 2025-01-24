using Microsoft.EntityFrameworkCore;

namespace ServerGame.Models
{
    public class GameContext : DbContext
    {
        public DbSet<GameResult> GameResults { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Host=localhost:5433;Database=TicTacToe;Username=postgres;Password=1234");
        }
    }
}
