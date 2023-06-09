using dotnet_docker_example.Configuration.Logging;
using Serilog;

var assemblyName = typeof(Program).Assembly.GetName().Name;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithProperty("Assembly", assemblyName)
    .WriteTo.Console()
    .WriteTo.Seq(serverUrl: "http://host.docker.internal:5341")
    .CreateLogger();

Log.Information("Starting web host...");

try
{
    var builder = WebApplication.CreateBuilder(args);

    //var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    //var caching = builder.Configuration.GetValue<string>("Caching");
    //Log.ForContext("ConnectionString", connectionString)
    //    .ForContext("Caching", caching)
    //    .Information("Loaded the connection string form configuration");

    var debugView = builder.Configuration.GetDebugView();
    Log.ForContext("ConfigurationDebugView", debugView)
        .Information("Configuration loaded");

    // Add services to the container.

    builder.Services.AddControllers();
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Host.UseSerilog();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseCustomRequestLogging();

    app.UseHttpsRedirection();

    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.Information("Host shut down complete");
    Log.CloseAndFlush();
}