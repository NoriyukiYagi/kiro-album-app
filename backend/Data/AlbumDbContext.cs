using Microsoft.EntityFrameworkCore;
using AlbumApp.Models;

namespace AlbumApp.Data;

public class AlbumDbContext : DbContext
{
    public AlbumDbContext(DbContextOptions<AlbumDbContext> options) : base(options)
    {
    }
    
    public DbSet<User> Users { get; set; }
    public DbSet<MediaFile> MediaFiles { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // User entity configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.GoogleId).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            
            // Configure DateTime properties for PostgreSQL
            entity.Property(e => e.CreatedAt)
                  .HasColumnType("timestamp with time zone")
                  .HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.LastLoginAt)
                  .HasColumnType("timestamp with time zone")
                  .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
        
        // MediaFile entity configuration
        modelBuilder.Entity<MediaFile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.FileName);
            entity.HasIndex(e => e.TakenAt);
            entity.HasIndex(e => e.UploadedAt);
            
            // Configure DateTime properties for PostgreSQL
            entity.Property(e => e.TakenAt)
                  .HasColumnType("timestamp with time zone");
            entity.Property(e => e.UploadedAt)
                  .HasColumnType("timestamp with time zone")
                  .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            // Foreign key relationship
            entity.HasOne(e => e.User)
                  .WithMany(u => u.MediaFiles)
                  .HasForeignKey(e => e.UploadedBy)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}