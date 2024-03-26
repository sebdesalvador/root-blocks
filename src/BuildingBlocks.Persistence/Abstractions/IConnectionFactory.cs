namespace BuildingBlocks.Persistence.Abstractions;

public interface IConnectionFactory
{
    string ConnectionString { get; }
    DbConnection CreateConnection();
}
