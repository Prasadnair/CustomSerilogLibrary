using SerilogHandler;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Add services to the container.
builder.Services.AddMemoryCache();

//builder.Services.AddSerilogConfiguration(); // Add the custom Serilog configuration services

await builder.UseCustomSerilogAsync();
//// Initialize Serilog configuration using the custom SerilogConfigurator
//var cache = new MemoryCache(new MemoryCacheOptions());
////var serilogConfigurator = new SerilogConfigurator(builder.Configuration, cache);
//var serilogConfigurator = new CustomSerilogConfigurator(builder.Configuration, cache);
//await serilogConfigurator.InitializeAsync(); // Ensure this is called before building the host

//await builder.UseSerilogWithConfigurationAsync();

//builder.Host.UseSerilog();

var app = builder.Build();
//app.UseSerilogRequestLogging(); // Log HTTP requests
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", (ILogger<Program> logger) =>
{
    logger.LogWarning("This is a warning log.");
    logger.LogInformation("This is a information log.");
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();


// Periodically refresh the cache
//var timer = new System.Threading.Timer(async _ =>
//{
//    await serilogConfigurator.InitializeAsync();
//}, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
