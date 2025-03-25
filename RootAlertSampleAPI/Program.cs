using RootAlert.Config;
using RootAlert.Middleware;
using RootAlert.MSSQL;
using RootAlert.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


var rootAlertOptions = new List<RootAlertOption>
{
    new TeamsAlertOption
    {
        AlertMethod = AlertType.Teams,
        WebhookUrl = "https://itshastra1.webhook.office.com/webhookb2/40f09ce9-ca1d-4105-a331-e7a95edd2d00@033c5e8c-b979-4b19-b7b5-6e1113d741af/IncomingWebhook/e78a8e83c1494a15b0426b89a145160b/007b8ed3-76a8-40c6-9fa5-63d75bb4a432/V2vsotinXwxmeUhvbX-niJ5InF23LFzJ_Uim66L_-1h9o1"
    },
    // new SlackAlertOption
    // {
    //     AlertMethod = AlertType.Slack,
    //     WebhookUrl = ""
    // }
};


var rootAlertSetting = new RootAlertSetting
{
    Storage = new RedisAlertStorage("127.0.0.1:6379"),
    BatchInterval = TimeSpan.FromSeconds(20),
    RootAlertOptions = rootAlertOptions,
};

// var rootAlertSetting = new RootAlertSetting
// {
//     Storage = new MSSQLAlertStorage("Server=localhost;Database=RootAlert;User Id=sa;Password=satsvelke;Encrypt=False;TrustServerCertificate=True;"),
//     BatchInterval = TimeSpan.FromSeconds(20),
//     RootAlertOptions = rootAlertOptions,
// };

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
