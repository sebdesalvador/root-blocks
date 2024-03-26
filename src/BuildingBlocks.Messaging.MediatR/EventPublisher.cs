namespace BuildingBlocks.Messaging.MediatR;

[ ExcludeFromCodeCoverage ]
public class EventPublisher : IEventPublisher
{
    private readonly ILogger< EventPublisher > _logger;
    private readonly IExecutionContext? _executionContext;
    private readonly IMediator _mediator;

    public EventPublisher( ILogger< EventPublisher > logger, IMediator mediator )
    {
        _logger = logger;
        _mediator = mediator;
    }

    public EventPublisher( ILogger< EventPublisher > logger, IExecutionContext executionContext, IMediator mediator )
    {
        _logger = logger;
        _executionContext = executionContext;
        _mediator = mediator;
    }

    public async Task PublishAsync( DomainEvent domainEvent, CancellationToken cancellationToken = default )
    {
        _logger.LogInformation( "Publishing {Event} event ({EventId})", domainEvent.TypeName, domainEvent.Id );

        if ( _executionContext is not null )
            domainEvent.SetCorrelationIds( _executionContext.CausationId, _executionContext.CorrelationId );

        await _mediator.Publish( domainEvent, cancellationToken );
    }
}
