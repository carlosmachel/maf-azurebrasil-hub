using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using RagDemo.Core.Entities;

namespace RagDemo.Core;

public interface IVectorStore
{
    Task UpsertFileAsync(FileRecord file, CancellationToken cancellationToken = default);

    Task ReplaceChunksAsync(string fileId, IReadOnlyList<(string Content, float[] Embedding)> chunks, CancellationToken cancellationToken = default);

    Task GrantAccessAsync(string userId, string fileId, CancellationToken cancellationToken = default);

    Task RevokeAccessAsync(string userId, string fileId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetAccessibleFileIdsAsync(string userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RetrievedChunk>> SearchAsync(string userId, float[] queryEmbedding, int topK, CancellationToken cancellationToken = default);
}

public sealed class EfVectorStore(string connectionString) : IVectorStore
{
    private readonly DbContextOptions<RagDemoDbContext> _options = RagDemoDbContextFactory.CreateOptions(connectionString);

    public async Task UpsertFileAsync(FileRecord file, CancellationToken cancellationToken = default)
    {
        await using var context = new RagDemoDbContext(_options);

        var existing = await context.Files.FindAsync([file.Id], cancellationToken);
        if (existing is null)
        {
            context.Files.Add(new FileEntity { Id = file.Id, Name = file.Name, Path = file.Path });
        }
        else
        {
            existing.Name = file.Name;
            existing.Path = file.Path;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task ReplaceChunksAsync(string fileId, IReadOnlyList<(string Content, float[] Embedding)> chunks, CancellationToken cancellationToken = default)
    {
        await using var context = new RagDemoDbContext(_options);

        await context.DocumentChunks
            .Where(c => c.FileId == fileId)
            .ExecuteDeleteAsync(cancellationToken);

        for (var i = 0; i < chunks.Count; i++)
        {
            context.DocumentChunks.Add(new DocumentChunkEntity
            {
                FileId = fileId,
                Content = chunks[i].Content,
                ChunkIndex = i,
                Embedding = new SqlVector<float>(chunks[i].Embedding)
            });
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task GrantAccessAsync(string userId, string fileId, CancellationToken cancellationToken = default)
    {
        await using var context = new RagDemoDbContext(_options);

        var exists = await context.FilePermissions
            .AnyAsync(p => p.UserId == userId && p.FileId == fileId, cancellationToken);

        if (!exists)
        {
            context.FilePermissions.Add(new FilePermissionEntity { UserId = userId, FileId = fileId });
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RevokeAccessAsync(string userId, string fileId, CancellationToken cancellationToken = default)
    {
        await using var context = new RagDemoDbContext(_options);

        await context.FilePermissions
            .Where(p => p.UserId == userId && p.FileId == fileId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetAccessibleFileIdsAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var context = new RagDemoDbContext(_options);

        return await context.FilePermissions
            .Where(p => p.UserId == userId)
            .Select(p => p.FileId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RetrievedChunk>> SearchAsync(string userId, float[] queryEmbedding, int topK, CancellationToken cancellationToken = default)
    {
        await using var context = new RagDemoDbContext(_options);

        var queryVector = new SqlVector<float>(queryEmbedding);

        var results = await context.DocumentChunks
            .Where(dc => context.FilePermissions.Any(p => p.UserId == userId && p.FileId == dc.FileId))
            .OrderBy(dc => EF.Functions.VectorDistance("cosine", dc.Embedding, queryVector))
            .Take(topK)
            .Select(dc => new
            {
                dc.Content,
                dc.File.Id,
                dc.File.Name,
                dc.File.Path,
                Distance = EF.Functions.VectorDistance("cosine", dc.Embedding, queryVector)
            })
            .ToListAsync(cancellationToken);

        return results
            .Select(r => new RetrievedChunk(
                Content: r.Content,
                FileId: r.Id,
                FileName: r.Name,
                FilePath: r.Path,
                Score: 1.0 - r.Distance))
            .ToList();
    }
}
