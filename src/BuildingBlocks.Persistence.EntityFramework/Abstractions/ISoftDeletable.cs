namespace BuildingBlocks.Persistence.EntityFramework.Abstractions;

public interface ISoftDeletable
{
    DateTime? DeletedOn { get; set; }
}
