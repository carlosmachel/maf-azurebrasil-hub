namespace RagDemo.Core;

public sealed record FileRecord(string Id, string Name, string Path);

public sealed record RetrievedChunk(string Content, string FileId, string FileName, string FilePath, double Score);
