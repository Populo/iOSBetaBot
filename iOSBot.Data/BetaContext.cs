using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace iOSBot.Data
{
    public class BetaContext : DbContext
    {
        public BetaContext(string dbtier = null)
        {
            if (null == dbtier)
            {
                dbtier = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            }

            var tier = dbtier switch
            {
                "Release" => "prod",
                _ => "dev"
            };

            _dbName = $"db_craigbot_{tier}";
            _dbUser = $"user_craigbot_{tier}";
        }

        private string _dbName { get; set; }
        private string _dbUser { get; set; }

        public DbSet<Server> Servers { get; set; }
        public DbSet<Thread> Threads { get; set; }
        public DbSet<Forum> Forums { get; set; }
        public DbSet<Config> Configs { get; set; }
        public DbSet<ErrorServer> ErrorServers { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder = null)
        {
            if (optionsBuilder.IsConfigured) return;

            var connectionString = new SqlConnectionStringBuilder
            {
                DataSource = "dale-server",
                InitialCatalog = _dbName,
                UserID = _dbUser,
                TrustServerCertificate = true,
                Encrypt = true,
                Password = File.ReadAllText("/run/secrets/dbPassBot")
            }.ConnectionString;

            optionsBuilder.UseSqlServer(connectionString,
                options =>
                {
                    options.EnableRetryOnFailure(
                        maxRetryCount: 20,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null
                    );
                });
        }
    }

    public class Server
    {
        public Guid Id { get; set; }
        public ulong ServerId { get; set; }
        public ulong ChannelId { get; set; }
        public Guid Track { get; set; }
        public string TagId { get; set; }
    }

    public class Forum
    {
        public Guid id { get; set; }
        public ulong ServerId { get; set; }
        public ulong ChannelId { get; set; }
        public Guid Track { get; set; }
    }

    public class Thread
    {
        public Guid id { get; set; }
        public ulong ServerId { get; set; }
        public ulong ChannelId { get; set; }
        public Guid Track { get; set; }
    }

    [PrimaryKey("Name")]
    public class Config
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class ErrorServer
    {
        public Guid Id { get; set; }
        public ulong ServerId { get; set; }
        public ulong ChannelId { get; set; }
    }
}