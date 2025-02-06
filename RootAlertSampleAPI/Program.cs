using RootAlert.Config;
using RootAlert.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


var rootOptions = new List<RootAlertOptions>();

rootOptions.Add(new RootAlertOptions
{
    AlertMethod = AlertType.Teams,
    BatchInterval = TimeSpan.FromSeconds(10),
    WebhookUrl = "web hook url"
});

rootOptions.Add(new RootAlertOptions
{
    AlertMethod = AlertType.Slack,
    BatchInterval = TimeSpan.FromSeconds(10),
    WebhookUrl = "web hook url"
});


builder.Services.AddRootAlert(rootOptions);






var app = builder.Build();

app.UseRootAlert();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}


app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{

    var i = 0;
    var p = 10 / i;

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
.WithName("GetWeatherForecast");


app.MapGet("/getuser", () =>
{
    throw new Exception("wheather api failed to call");
});

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
