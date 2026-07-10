using Microsoft.EntityFrameworkCore;
namespace Arora.Workflow.EntityFramework;
internal class DbContextProvider
{
    public DbContext Context { get; }
    public DbContextProvider(DbContext context) => Context = context;
}
