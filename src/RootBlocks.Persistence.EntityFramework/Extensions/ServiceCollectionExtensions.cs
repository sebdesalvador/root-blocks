namespace RootBlocks.Persistence.EntityFramework.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the outbox of <typeparamref name="TContext" />: the <see cref="OutboxPublisher" /> its unit of work
    /// publishes with, and an <see cref="OutboxDispatcher{TContext}" /> that publishes what the unit of work could
    /// not. Call it once per context; <see cref="OutboxOptions" /> are bound from the <c>Outbox</c> section of
    /// <paramref name="configuration" />.
    /// </summary>
    /// <remarks>
    /// <typeparamref name="TContext" /> must map the outbox with
    /// <see cref="ModelBuilderExtensions.ApplyOutbox" />, and an <see cref="IEventPublisher" /> must be registered.
    /// </remarks>
    public static IServiceCollection AddOutbox< TContext >( this IServiceCollection services, IConfiguration configuration )
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull( configuration );

        services.AddOptions< OutboxOptions >().Bind( configuration.GetSection( OutboxOptions.SECTION ) );
        services.TryAddSingleton( TimeProvider.System );
        services.TryAddScoped< OutboxPublisher >();
        services.AddHostedService< OutboxDispatcher< TContext > >();
        return services;
    }

    [ Obsolete( "The Service Broker listener is gone: this now registers an OutboxDispatcher for the context registered as "
              + "IDatabaseContextWithOutbox. Call AddOutbox< TContext >( configuration ) instead." ) ]
    public static IServiceCollection AddOutboxListener(
        this IServiceCollection services,
        IConfigurationSection? outboxWatcherSection = null
    )
    {
        if ( outboxWatcherSection is not null )
            services.AddOptions< OutboxOptions >().Bind( outboxWatcherSection );

        services.TryAddScoped( sp => ( DatabaseContextWithOutbox )sp.GetRequiredService< IDatabaseContextWithOutbox >() );
        services.TryAddSingleton( TimeProvider.System );
        services.TryAddScoped< OutboxPublisher >();
        services.AddHostedService< OutboxDispatcher< DatabaseContextWithOutbox > >();
        return services;
    }
}
