using Microsoft.EntityFrameworkCore;
using RagDemo.Core.Entities;

namespace RagDemo.Core;

public sealed class RagDemoDbContext(DbContextOptions<RagDemoDbContext> options) : DbContext(options)
{
    public DbSet<FileEntity> Files => Set<FileEntity>();
    public DbSet<DocumentChunkEntity> DocumentChunks => Set<DocumentChunkEntity>();
    public DbSet<FilePermissionEntity> FilePermissions => Set<FilePermissionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FileEntity>(file =>
        {
            file.ToTable("Files", "dbo");
            file.HasKey(f => f.Id);
            file.Property(f => f.Id).HasMaxLength(50);
            file.Property(f => f.Name).HasMaxLength(500).IsRequired();
            file.Property(f => f.Path).HasMaxLength(2000).IsRequired();
            file.Property(f => f.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<DocumentChunkEntity>(chunk =>
        {
            chunk.ToTable("DocumentChunks", "dbo");
            chunk.HasKey(c => c.Id);
            chunk.Property(c => c.Id).HasDefaultValueSql("NEWID()");
            chunk.Property(c => c.FileId).HasMaxLength(50).IsRequired();
            chunk.Property(c => c.Embedding).HasColumnType("vector(1536)");
            chunk.HasIndex(c => c.FileId);

            chunk.HasOne(c => c.File)
                .WithMany(f => f.Chunks)
                .HasForeignKey(c => c.FileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FilePermissionEntity>(permission =>
        {
            permission.ToTable("FilePermissions", "dbo");
            permission.HasKey(p => new { p.UserId, p.FileId });
            permission.Property(p => p.UserId).HasMaxLength(50);
            permission.Property(p => p.FileId).HasMaxLength(50);

            permission.HasOne(p => p.File)
                .WithMany(f => f.Permissions)
                .HasForeignKey(p => p.FileId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
