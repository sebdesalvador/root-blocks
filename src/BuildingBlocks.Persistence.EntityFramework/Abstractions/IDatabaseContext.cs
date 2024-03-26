namespace BuildingBlocks.Persistence.EntityFramework.Abstractions;

public interface IDatabaseContext
{
    string DefaultSchema { get; }
}
