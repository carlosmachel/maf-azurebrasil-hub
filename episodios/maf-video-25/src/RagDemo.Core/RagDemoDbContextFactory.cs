using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RagDemo.Core;

public static class RagDemoDbContextFactory
{
    public static DbContextOptions<RagDemoDbContext> CreateOptions(string connectionString) =>
        new DbContextOptionsBuilder<RagDemoDbContext>()
            .UseSqlServer(connectionString)
            .Options;
}
