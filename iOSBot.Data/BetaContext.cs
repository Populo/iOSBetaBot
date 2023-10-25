using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace iOSBot.Data
{
    public class BetaContext : DbContext
    {
        public DbSet<Update> Updates { get; set; }
        public DbSet<Server> Servers { get; set; }
        public DbSet<Device> Devices { get; set; }
        public DbSet<Config> Configs { get; set; }
        public DbSet<ErrorServer> ErrorServers { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder = null)
        {
            if (!optionsBuilder.IsConfigured) {
                var connection = new MySqlConnectionStringBuilder();
                connection.Server = "pinas";

#if DEBUG
                connection.Database = "iOSBetaDev";
#else
                connection.Database = "iOSBeta";
#endif

                connection.UserID = "BetaBot";
                connection.Password = Environment.GetEnvironmentVariable("BetaBotDbPass");

                connection.ForceSynchronous = true;

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
}