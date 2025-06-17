using iOSBot.Console.Migrations;
using Microsoft.EntityFrameworkCore;
using Thread = iOSBot.Console.Migrations.Thread;

namespace iOSBot.Console.Context;

public partial class MyDbContext : DbContext
{
    public MyDbContext()
    {
    }

    public MyDbContext(DbContextOptions<MyDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Config> Configs { get; set; }

    public virtual DbSet<Device> Devices { get; set; }

    public virtual DbSet<EfmigrationsHistory> EfmigrationsHistories { get; set; }

    public virtual DbSet<ErrorServer> ErrorServers { get; set; }

    public virtual DbSet<Forum> Forums { get; set; }

    public virtual DbSet<Release> Releases { get; set; }

    public virtual DbSet<Server> Servers { get; set; }

    public virtual DbSet<Thread> Threads { get; set; }

    public virtual DbSet<Update> Updates { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https: //go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseMySql("server=dale-server;database=iOSBeta;uid=BetaBot;pwd=",
            ServerVersion.Parse("10.11.13-mariadb"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_general_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Config>(entity => { entity.HasKey(e => e.Name).HasName("PRIMARY"); });

        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasKey(e => e.AudienceId).HasName("PRIMARY");

            entity.Property(e => e.Color).HasColumnType("int(10) unsigned");
            entity.Property(e => e.Priority).HasColumnType("int(11)");
        });

        modelBuilder.Entity<EfmigrationsHistory>(entity =>
        {
            entity.HasKey(e => e.MigrationId).HasName("PRIMARY");

            entity.ToTable("__EFMigrationsHistory");

            entity.Property(e => e.MigrationId).HasMaxLength(150);
            entity.Property(e => e.ProductVersion).HasMaxLength(32);
        });

        modelBuilder.Entity<ErrorServer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.Id)
                .UseCollation("ascii_general_ci")
                .HasCharSet("ascii");
            entity.Property(e => e.ChannelId).HasColumnType("bigint(20) unsigned");
            entity.Property(e => e.ServerId).HasColumnType("bigint(20) unsigned");
        });

        modelBuilder.Entity<Forum>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .UseCollation("ascii_general_ci")
                .HasCharSet("ascii");
            entity.Property(e => e.ChannelId).HasColumnType("bigint(20) unsigned");
            entity.Property(e => e.ServerId).HasColumnType("bigint(20) unsigned");
        });

        modelBuilder.Entity<Release>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.Id)
                .UseCollation("ascii_general_ci")
                .HasCharSet("ascii");
            entity.Property(e => e.Date).HasMaxLength(6);
            entity.Property(e => e.WaitTime).HasColumnType("int(11)");
        });

        modelBuilder.Entity<Server>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.Id)
                .UseCollation("ascii_general_ci")
                .HasCharSet("ascii");
            entity.Property(e => e.ChannelId).HasColumnType("bigint(20) unsigned");
            entity.Property(e => e.ServerId).HasColumnType("bigint(20) unsigned");
        });

        modelBuilder.Entity<Thread>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .UseCollation("ascii_general_ci")
                .HasCharSet("ascii");
            entity.Property(e => e.ChannelId).HasColumnType("bigint(20) unsigned");
            entity.Property(e => e.ServerId).HasColumnType("bigint(20) unsigned");
        });

        modelBuilder.Entity<Update>(entity =>
        {
            entity.HasKey(e => e.Guid).HasName("PRIMARY");

            entity.Property(e => e.Guid)
                .UseCollation("ascii_general_ci")
                .HasCharSet("ascii");
            entity.Property(e => e.ReleaseDate)
                .HasMaxLength(6)
                .HasDefaultValueSql("'0001-01-01 00:00:00.000000'");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}