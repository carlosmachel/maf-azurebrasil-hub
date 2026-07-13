namespace RagDemo.Core.Entities;

public sealed class FilePermissionEntity
{
    public string UserId { get; set; } = null!;
    public string FileId { get; set; } = null!;
    public FileEntity File { get; set; } = null!;
}
