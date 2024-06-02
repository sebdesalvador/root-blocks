namespace RootBlocks.Persistence.EntityFramework.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOutboxListener(
        this IServiceCollection services,
        IConfigurationSection? outboxWatcherSection = null
    ) => services.AddHostedService< OutboxListener >();
}
