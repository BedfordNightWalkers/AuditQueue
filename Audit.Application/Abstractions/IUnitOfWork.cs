namespace Audit.Application.Abstractions;

public interface IUnitOfWork
{
    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken);

}