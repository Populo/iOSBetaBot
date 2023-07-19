﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using iOSBot.Data;

#nullable disable

namespace iOSBot.Data.Migrations
{
    [DbContext(typeof(BetaContext))]
    [Migration("20230716211634_rework3")]
    partial class rework3
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.9")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("iOSBot.Data.Category", b =>
                {
                    b.Property<string>("Name")
                        .HasColumnType("varchar(255)");

                    b.HasKey("Name");

                    b.ToTable("Categories");
                });

            modelBuilder.Entity("iOSBot.Data.Server", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<string>("CategoryId")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<ulong>("ChannelId")
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong>("ServerId")
                        .HasColumnType("bigint unsigned");

                    b.HasKey("Id");

                    b.HasIndex("CategoryId");

                    b.ToTable("Servers");
                });

            modelBuilder.Entity("iOSBot.Data.Update", b =>
                {
                    b.Property<Guid>("Guid")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<string>("Build")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("CategoryId")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<string>("Version")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("Guid");

                    b.HasIndex("CategoryId");

                    b.ToTable("Updates");
                });

            modelBuilder.Entity("iOSBot.Data.Server", b =>
                {
                    b.HasOne("iOSBot.Data.Category", "Category")
                        .WithMany("Servers")
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Category");
                });

            modelBuilder.Entity("iOSBot.Data.Update", b =>
                {
                    b.HasOne("iOSBot.Data.Category", "Category")
                        .WithMany("Updates")
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Category");
                });

            modelBuilder.Entity("iOSBot.Data.Category", b =>
                {
                    b.Navigation("Servers");

                    b.Navigation("Updates");
                });
#pragma warning restore 612, 618
        }
    }
}
