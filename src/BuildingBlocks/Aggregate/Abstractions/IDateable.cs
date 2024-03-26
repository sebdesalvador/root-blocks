namespace BuildingBlocks.Aggregate.Abstractions;

public interface IDatable
{
    DateTime CreatedOn { get; set; }
    DateTime LastModifiedOn { get; set; }
}
