namespace RootBlocks.Messaging.MediatR.Extensions;

[ ExcludeFromCodeCoverage ]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMessaging( this IServiceCollection services )
        => services.AddScoped< IEventPublisher, EventPublisher >();
}
