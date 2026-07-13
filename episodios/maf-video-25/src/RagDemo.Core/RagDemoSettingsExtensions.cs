using Microsoft.EntityFrameworkCore;

namespace RagDemo.Core;

public static class RagDemoSettingsExtensions
{
    public static async Task ApplyMigrationsAsync(this RagDemoSettings settings, CancellationToken cancellationToken = default)
    {
        await using var context = new RagDemoDbContext(RagDemoDbContextFactory.CreateOptions(settings.SqlConnectionString));
        await context.Database.MigrateAsync(cancellationToken);
    }
}
