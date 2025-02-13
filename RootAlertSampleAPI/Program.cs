using RootAlert.Config;
using RootAlert.Middleware;
using RootAlert.Storage.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


var rootAlertOptions = new List<RootAlertOption>
{
    new RootAlertOption
    {
        AlertMethod = AlertType.Teams,
        WebhookUrl = ""
    },
    new RootAlertOption
    {
        AlertMethod = AlertType.Slack,
        WebhookUrl = ""
    }
};


var rootAlertSetting = new RootAlertSetting
{
    Storage = new RedisAlertStorage("127.0.0.1:6379"),
    BatchInterval = TimeSpan.FromSeconds(20),
    RootAlertOptions = rootAlertOptions,
};

builder.Services.AddRootAlert(rootAlertSetting);



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
