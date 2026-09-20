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
    // AddControllers() used to register these implicitly; minimal APIs need them stated.
    builder.Services.AddAuthentication();
    builder.Services.AddAuthorization();
    builder.Services.ConfigureHttpJsonOptions(
        o => o.SerializerOptions.Converters.Add( new JsonStringEnumConverter() )
    );
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddOpenApi( "v1", o =>
    {
        o.AddStronglyTypedIds();
        o.AddJsonPatchExamples();
        o.AddDocumentTransformer( ( document, _, _ ) =>
        {
            document.Info = new OpenApiInfo { Title = "Blogs API", Version = "v0.0.0" };
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
    app.MapBlogEndpoints();
    app.MapPersonEndpoints();
    app.MapPostEndpoints();
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
