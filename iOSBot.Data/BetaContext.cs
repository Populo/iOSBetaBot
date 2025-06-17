using Microsoft.EntityFrameworkCore;
using MySqlConnector;

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

            switch (dbtier)
            {
                case "Release":
                    DbName = "CraigBot";
                    DbUser = "BetaBot";
                    break;
                default:
                    DbName = "CraigBotDev";
                    DbUser = "BetaBotDev";
                    break;
            }
        }

        private string DbName { get; set; }
        private string DbUser { get; set; }

        public DbSet<Server> Servers { get; set; }
        public DbSet<Thread> Threads { get; set; }
        public DbSet<Forum> Forums { get; set; }
        public DbSet<Config> Configs { get; set; }
        public DbSet<ErrorServer> ErrorServers { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder = null)
        {
            if (optionsBuilder.IsConfigured) return;

            var connection = new MySqlConnectionStringBuilder();
            connection.Server = "dale-server";

            connection.Database = DbName;

            connection.UserID = DbUser;

            //connection.Password = "";
            connection.Password = File.ReadAllText("/run/secrets/dbPass");

            //connection.ForceSynchronous = true;

            optionsBuilder.UseMySql(connection.ConnectionString,
                ServerVersion.AutoDetect(connection.ConnectionString),
                options =>
                {
                    options.EnableRetryOnFailure(20, TimeSpan.FromSeconds(10), new List<int>());
                    options.CommandTimeout(600);
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