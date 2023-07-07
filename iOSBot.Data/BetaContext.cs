using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace iOSBot.Data
{
    public class BetaContext : DbContext
    {
        public DbSet<Post> Posts { get; set; }
        public DbSet<Server> Servers { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured) {
                var connection = new MySqlConnectionStringBuilder();
                connection.Server = "pinas";
                connection.UserID = "BetaBot";
                connection.Password = "1UwDO;FHwz{*Z+@IJY4a";

#if DEBUG
                connection.Database = "BetaBotDev";
#else
                connection.Database = "BetaBot";
#endif

                optionsBuilder.UseMySql(connection.ConnectionString, ServerVersion.AutoDetect(connection.ConnectionString));
            }
        }
    }

    public class Post
    {
        public string Guid { get; set; }
        public DateTime PostDate { get; set; }
    }

    public class Server
    {
        public ulong ServerId { get; set; }
        public ulong ChannelId { get; set; }
    }
}