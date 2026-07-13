using Microsoft.Data.SqlTypes;

namespace RagDemo.Core.Entities;

public sealed class DocumentChunkEntity
{
    public Guid Id { get; set; }
    public string FileId { get; set; } = null!;
    public FileEntity File { get; set; } = null!;
    public string Content { get; set; } = null!;
    public int ChunkIndex { get; set; }
    public SqlVector<float> Embedding { get; set; }
}
