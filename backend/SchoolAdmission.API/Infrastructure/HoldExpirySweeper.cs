using SchoolAdmission.Application.Abstractions;

namespace SchoolAdmission.API.Infrastructure;

// Every few seconds, marks expired HELD rows as EXPIRED.
// The expiry time lives in the database, so this works after a restart too.
public class HoldExpirySweeper : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<HoldExpirySweeper> _logger;

    public HoldExpirySweeper(IServiceScopeFactory scopeFactory, ILogger<HoldExpirySweeper> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<IAdmissionService>();
                    var count = await service.ExpireStaleHoldsAsync(stoppingToken);
                    if (count > 0) _logger.LogInformation("Expired {Count} stale hold(s)", count);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Hold expiry sweep failed");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
    }
}
