using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace iOSBot.Data;

public class InternContext : DbContext
{
    private string DbUser;

    public InternContext(string? dbtier = null)
    {
        dbtier ??= Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        DbUser = dbtier switch
        {
            "Release" => "BetaBot",
            _ => "BetaBotDev"
        };
    }

    public DbSet<Update> Updates { get; set; }
    public DbSet<Track> Tracks { get; set; }
    public DbSet<Feed> Feeds { get; set; }
    public DbSet<InternConfig> Configs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var connection = new MySqlConnectionStringBuilder();
            connection.Server = "dale-server";
            connection.Database = "CraigsIntern";
            connection.UserID = DbUser;
            connection.Password = File.ReadAllText("/run/secrets/dbPass");

            optionsBuilder.UseMySql(connection.ConnectionString,
                ServerVersion.AutoDetect(connection.ConnectionString),
                options =>
                {
                    options.EnableRetryOnFailure(20, TimeSpan.FromSeconds(10), new List<int>());
                    options.CommandTimeout(600);
                });
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Update>().Navigation(e => e.Track).AutoInclude();
        modelBuilder.Entity<Track>().Navigation(e => e.Feeds).AutoInclude();
        modelBuilder.Entity<Feed>().Navigation(e => e.Track).AutoInclude();
    }
}

public class Update
{
    public Guid UpdateId { get; set; }
    public Guid TrackId { get; set; }
    public string Version { get; set; }
    public string Build { get; set; }
    public DateTime ReleaseDate { get; set; }
    public string Hash { get; set; }
    public string SUDocId { get; set; }

    public Track Track { get; set; }
}

public class Track
{
    public Guid TrackId { get; set; }
    public string Name { get; set; }
    public string ReleaseType { get; set; }
    public uint Color { get; set; }

    public List<Feed> Feeds { get; set; }
}

public class Feed
{
    public Guid FeedId { get; set; }
    public Guid TrackId { get; set; }

    public string DeviceBoard { get; set; } // D94AP
    public string DeviceIdentifier { get; set; } // iPhone17,2 
    public string FwVersion { get; set; } // 26.0
    public string FwBuild { get; set; } // 23A5260u
    public string Name { get; set; }
    public string Audience { get; set; }
    public string AssetType { get; set; }

    public Track Track { get; set; }
}

[PrimaryKey("Name"), Table("Config")]
public class InternConfig
{
    public string Name { get; set; }
    public string Value { get; set; }
}