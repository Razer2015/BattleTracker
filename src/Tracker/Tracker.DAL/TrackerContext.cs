using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Tracker.DAL.Models;

namespace Tracker.DAL
{
    public partial class TrackerContext : DbContext
    {
        public virtual DbSet<TrackerEntry> TrackerEntries { get; set; }

        public TrackerContext() { }
        public TrackerContext(DbContextOptions<TrackerContext> options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //if (!optionsBuilder.IsConfigured)
            //{
            //    optionsBuilder.UseMySql(ServerVersion.AutoDetect());
            //}
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TrackerEntry>(entity => {
                entity.HasKey(e => new { e.Id })
                    .HasName("PRIMARY KEY");

                entity.ToTable("bt_trackers");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.HasIndex(e => e.PersonaId)
                    .HasDatabaseName("bt_trackers_personaid_index")
                    .IsUnique();

                entity.Property(e => e.PersonaId)
                    .HasColumnName("personaId");

                entity.Property(e => e.SoldierName)
                    .HasColumnName("soldierName")
                    .HasColumnType("text");

                entity.Property(e => e.Tag)
                    .HasColumnName("tag")
                    .HasColumnType("text");

                entity.Property(e => e.Reason)
                    .HasColumnName("reason")
                    .HasColumnType("text");
            });
        }
    }
}
