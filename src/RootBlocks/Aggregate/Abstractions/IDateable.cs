namespace RootBlocks.Aggregate.Abstractions;

public interface IDatable
{
    DateTime CreatedOn { get; set; }
    DateTime LastModifiedOn { get; set; }
}
