using Audit.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Audit.Tests.Data;

// SQLite in-memory database with the AuditDbContext schema. Preferred over the EF
// InMemory provider because it enforces constraints such as unique indexes.
// The database lives as long as this instance keeps its connection open.
public sealed class SqliteAuditDatabase : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly DbContextOptions<AuditDbContext> _options;

    public SqliteAuditDatabase()
    {
        _connection.Open();
        _options = new DbContextOptionsBuilder<AuditDbContext>()
            .UseSqlite(_connection)
            .ReplaceService<IModelCustomizer, SqliteCompatibleModelCustomizer>()
            .Options;

        using var dbContext = CreateContext();
        dbContext.Database.EnsureCreated();
    }

    public AuditDbContext CreateContext() => new(_options);

    public void Dispose() => _connection.Dispose();

    // AuditDbContext maps PayloadJson as nvarchar(max), which SQLite cannot parse;
    // drop SQL Server-specific column types so EnsureCreated can build the schema.
    private sealed class SqliteCompatibleModelCustomizer(ModelCustomizerDependencies dependencies)
        : RelationalModelCustomizer(dependencies)
    {
        public override void Customize(ModelBuilder modelBuilder, DbContext context)
        {
            base.Customize(modelBuilder, context);

            foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetProperties()))
            {
                if (property.GetColumnType()?.Contains("(max)", StringComparison.OrdinalIgnoreCase) == true)
                {
                    property.SetColumnType(null);
                }
            }
        }
    }
}
