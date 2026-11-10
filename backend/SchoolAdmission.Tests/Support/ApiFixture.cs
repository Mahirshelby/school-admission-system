using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Testcontainers.PostgreSql;

namespace SchoolAdmission.Tests.Support;

// Starts one real PostgreSQL container and the real API on top of it.
// All tests share it, so the container is started only once per test run.
public sealed class ApiFixture : IAsyncLifetime
{
    public const int HoldDurationSeconds = 600;

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16").Build();
    private TestFactory? _factory;

    // Tests move this clock forward to make holds expire, instead of waiting in real time.
    public FakeTimeProvider Clock { get; } = new(DateTimeOffset.UtcNow);

    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _factory = new TestFactory(_postgres.GetConnectionString(), Clock, HoldDurationSeconds);

        // Creating the client starts the app, which applies the EF Core migrations to the empty database.
        Client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }
        await _postgres.DisposeAsync();
    }

    private sealed class TestFactory : WebApplicationFactory<Program>
    {
        private readonly string _connectionString;
        private readonly TimeProvider _clock;
        private readonly int _holdSeconds;

        public TestFactory(string connectionString, TimeProvider clock, int holdSeconds)
        {
            _connectionString = connectionString;
            _clock = clock;
            _holdSeconds = holdSeconds;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Added last, so these values win over appsettings.json.
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Default"] = _connectionString,
                    ["HOLD_DURATION_SECONDS"] = _holdSeconds.ToString()
                }));

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton(_clock);
            });
        }
    }
}
