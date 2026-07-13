namespace RagDemo.Core.Entities;

public sealed class FileEntity
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Path { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public List<DocumentChunkEntity> Chunks { get; set; } = [];
    public List<FilePermissionEntity> Permissions { get; set; } = [];
}
