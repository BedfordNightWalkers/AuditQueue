using Audit.Application.Abstractions;

namespace Audit.Infrastructure.Data;

public sealed class UnitOfWork (AuditDbContext dbContext) : IUnitOfWork
{
    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await operation(cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}