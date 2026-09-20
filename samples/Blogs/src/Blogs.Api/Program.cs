Log.Logger = new LoggerConfiguration().MinimumLevel.Override( "Microsoft", LogEventLevel.Warning )
                                      .Enrich.FromLogContext()
                                      .WriteTo.Console()
                                      .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder( args );
    builder.Host.UseSerilog(
        ( context, _, configuration ) =>
            configuration.ReadFrom.Configuration( context.Configuration )
    );

    // Options
    builder.Services.Configure< RouteOptions >( o => o.LowercaseUrls = true );

    // Services
    // builder.Services.AddLogContext();
    builder.Services.AddHealthChecks();
    builder.Services.AddCors( o => o.AddDefaultPolicy( b => b.WithOrigins( "http://localhost:4040" )
                                                             .WithHeaders( "Authorization" ) ) );
    builder.Services
           .AddControllers( o =>
            {
                // This is required to support JSON PATCH requests
                o.InputFormatters.Insert( 0, NewtonsoftJsonPatchInputFormatterHelper.GetJsonPatchInputFormatter() );
            } )
           .AddJsonOptions( o => o.JsonSerializerOptions.Converters.Add( new JsonStringEnumConverter() ) );
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddOpenApi( "v1", o =>
    {
        // Registers the RootBlocks transformers: strongly-typed ids as uuid strings, JSON Patch examples.
        o.AddRootBlocks();
        o.AddDocumentTransformer( ( document, _, _ ) =>
        {
            document.Info = new OpenApiInfo { Title = "Blogs API", Version = "v0.0.0" };
            return Task.CompletedTask;
        } );
        o.AddOperationTransformer( ( operation, context, _ ) =>
        {
            // Mirrors the camelCased operation ids Swashbuckle used to produce from the action name.
            if ( context.Description.ActionDescriptor is ControllerActionDescriptor descriptor )
                operation.OperationId = char.ToLowerInvariant( descriptor.ActionName[ 0 ] )
                                      + descriptor.ActionName[ 1.. ];

            return Task.CompletedTask;
        } );
    } );
    builder.Services.AddResponseCompression( o => o.Providers.Add< BrotliCompressionProvider >() );
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure( builder.Configuration.GetConnectionString( "DefaultConnection" )! );

    // Middleware
    var app = builder.Build();
    // app.UseLogsCorrelator();
    // app.UseGlobalExceptionHandling();
    // app.UseCorrelationHeaders();
    app.UseHttpsRedirection();
    app.UseCors( p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod() );
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseResponseCompression();
    app.MapControllers();
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.MapHealthChecks( "/health" );
    app.Run();
}
catch ( Exception e )
{
    Log.Fatal( e, "An unhandled exception occured during bootstrapping" );
}
finally
{
    Log.CloseAndFlush();
}
