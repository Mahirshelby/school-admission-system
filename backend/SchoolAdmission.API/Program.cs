using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolAdmission.API.Infrastructure;
using SchoolAdmission.Application.Abstractions;
using SchoolAdmission.Application.Options;
using SchoolAdmission.Infrastructure.Persistence;
using SchoolAdmission.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Hold duration comes from the HOLD_DURATION_SECONDS environment variable (default 600).
builder.Services.AddOptions<AdmissionOptions>().Configure<IConfiguration>((options, config) =>
{
    var seconds = config.GetValue<int?>("HOLD_DURATION_SECONDS");
    options.HoldDurationSeconds = seconds is > 0 ? seconds.Value : AdmissionOptions.DefaultHoldDurationSeconds;
});

builder.Services.AddDbContext<AdmissionDbContext>((sp, options) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connectionString = config.GetConnectionString("Default")
        ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");
    options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();
});

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IAdmissionService, AdmissionService>();
builder.Services.AddHostedService<HoldExpirySweeper>();

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .ConfigureApiBehaviorOptions(o =>
    {
        // Bad or missing request bodies return our standard error shape.
        o.InvalidModelStateResponseFactory = _ => new BadRequestObjectResult(
            new ErrorResponse("invalid_request", "The request is invalid or malformed."));
    });

// No login or cookies, so allowing any origin is safe here. It lets the React app call the API.
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

// Apply EF Core migrations on startup. Retry because the database may still be starting.
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AdmissionDbContext>();
    const int maxAttempts = 15;
    for (var attempt = 1; ; attempt++)
    {
        try
        {
            await db.Database.MigrateAsync();
            break;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            app.Logger.LogWarning("Database not ready (attempt {Attempt}/{Max}): {Message}", attempt, maxAttempts, ex.Message);
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }
}

app.Run();

// Lets the test project start the app with WebApplicationFactory<Program>.
public partial class Program { }
