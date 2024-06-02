namespace RootBlocks.Serialization.Tests;

public class TestDomainEventWithoutAttribute : DomainEvent
{
    public string Primitive { get; init; } = "Primitive";
    public required Complex Complex { get; init; }
}
