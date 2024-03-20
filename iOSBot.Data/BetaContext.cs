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
                    DbName = "iOSBeta";
                    break;
                case "Develop":
                    DbName = "iOSBetaDev";
                    break;
            }
        }

        private string DbName { get; set; }
        
        public DbSet<Update> Updates { get; set; }
        public DbSet<Server> Servers { get; set; }
        public DbSet<Thread> Threads { get; set; }
        public DbSet<Device> Devices { get; set; }
        public DbSet<Config> Configs { get; set; }
        public DbSet<ErrorServer> ErrorServers { get; set; }
        public DbSet<Release> Releases { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder = null)
        {
            if (!optionsBuilder.IsConfigured) {
                var connection = new MySqlConnectionStringBuilder();
                connection.Server = "pinas";

                connection.Database = DbName;

                connection.UserID = "BetaBot";
                connection.Password = Environment.GetEnvironmentVariable("BetaBotDbPass");

                //connection.ForceSynchronous = true;

                optionsBuilder.UseMySql(connection.ConnectionString, ServerVersion.AutoDetect(connection.ConnectionString),
                    options =>
                    {
                        options.EnableRetryOnFailure(20, TimeSpan.FromSeconds(10), new List<int>());
                    });
            }
        }
    }

    [PrimaryKey(nameof(Guid))]
    public class Update
    {
        public Guid Guid { get; set; }
        public string Version { get; set; }
        public string Build { get; set; }
        public string Category { get; set; }
        public DateTime ReleaseDate { get; set; }
    }

    public class Server
    {
        public Guid Id { get; set; }
        public ulong ServerId { get; set; }
        public ulong ChannelId { get; set; }
        public string Category { get; set; }
        public string TagId { get; set; }
    }

    public class Thread
    {
        public Guid id { get; set; }
        public ulong ServerId { get; set; }
        public ulong ChannelId { get; set; }
        public string Category { get; set; }
    }

    [PrimaryKey("AudienceId")]
    public class Device
    {
        public string AudienceId { get; set; }
        public string Name { get; set; }
        public string FriendlyName { get; set; }
        public string Version { get; set; }
        public string BuildId { get; set; }
        public string Product { get; set; }
        public string BoardId { get; set; }
        public string Category { get; set; }
        public string Changelog { get; set; }
        // Developer, Public, Release
        public string Type { get; set; }
        public uint Color { get; set; }
        public string AssetType { get; set; }
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

    public class Release
    {
        public Guid Id { get; set; }
        public string Major { get; set; }
        public string Minor { get; set; }
        public string Beta { get; set; }
        public DateTime Date { get; set; }
        public int WaitTime { get; set; }
    }
}