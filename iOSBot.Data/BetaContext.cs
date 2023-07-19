using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace iOSBot.Data
{
    public class BetaContext : DbContext
    {
        public DbSet<Update> Updates { get; set; }
        public DbSet<Server> Servers { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured) {
                var connection = new MySqlConnectionStringBuilder();
                connection.Server = "pinas";
                connection.UserID = "BetaBot";
                connection.Password = "h_XUs6g!Q8aE2pL-wpta";

#if DEBUG
                connection.Database = "iOSBetaDev";
#else
                connection.Database = "iOSBeta";
#endif

                optionsBuilder.UseMySql(connection.ConnectionString, ServerVersion.AutoDetect(connection.ConnectionString));
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }

    [PrimaryKey(nameof(Guid))]
    public class Update
    {
        public Guid Guid { get; set; }
        public string Version { get; set; }
        public string Build { get; set; }
        public string Category { get; set; }
    }

    public class Server
    {
        public Guid Id { get; set; }
        public ulong ServerId { get; set; }
        public ulong ChannelId { get; set; }
        public string Category { get; set; }
    }
}