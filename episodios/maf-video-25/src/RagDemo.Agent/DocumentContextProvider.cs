using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using RagDemo.Core;

namespace RagDemo.Agent;

internal sealed class DocumentContextProvider(
    IPermissionService permissionService,
    IVectorStore vectorStore,
    IEmbeddingService embeddingService)
    : AIContextProvider
{
    private const int TopK = 4;

    protected override async ValueTask<AIContext> ProvideAIContextAsync(InvokingContext context, CancellationToken cancellationToken = default)
    {
        var stateBag = context.Session?.StateBag;
        if (stateBag is null || !stateBag.TryGetValue<string>("UserId", out var userId) || string.IsNullOrEmpty(userId))
        {
            return new AIContext
            {
                Instructions = "No user identity is set on this session. Refuse to answer and ask the user to sign in first."
            };
        }

        var query = context.AIContext.Messages?.LastOrDefault(m => m.Role == ChatRole.User)?.Text;
        if (string.IsNullOrWhiteSpace(query))
        {
            return new AIContext();
        }

        var accessibleFileIds = await permissionService.GetAccessibleFileIdsAsync(userId, cancellationToken);
        if (accessibleFileIds.Count == 0)
        {
            return new AIContext
            {
                Instructions = $"User '{userId}' does not currently have access to any documents. Say so plainly and do not fabricate an answer."
            };
        }

        var queryEmbedding = await embeddingService.EmbedAsync(query, cancellationToken);
        var chunks = await vectorStore.SearchAsync(userId, queryEmbedding, TopK, cancellationToken);

        if (chunks.Count == 0)
        {
            return new AIContext
            {
                Instructions = $"No relevant content was found in the documents user '{userId}' has access to. Say so plainly and do not fabricate an answer."
            };
        }

        var contextBlock = string.Join("\n\n", chunks.Select(c => $"[Source: {c.FileName}]\n{c.Content}"));

        return new AIContext
        {
            Instructions = $"""
                Answer using only the following retrieved context. Always cite the source file name when you use it.

                {contextBlock}
                """
        };
    }
}
