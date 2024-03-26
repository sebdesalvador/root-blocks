namespace BuildingBlocks.Persistence.Abstractions;

public interface IUnitOfWork
{
    void RegisterNew( IAggregateRoot aggregateRoot );
    void RegisterClean( IAggregateRoot aggregateRoot );
    void RegisterDirty( IAggregateRoot aggregateRoot );
    void RegisterDeleted( IAggregateRoot aggregateRoot );
    void Commit();
    Task CommitAsync( CancellationToken cancellationToken = default );
    void Rollback();
    void BeginTransaction();
    Task BeginTransactionAsync();
    void CommitTransaction();
    Task CommitTransactionAsync();
    void RollbackTransaction();
}
