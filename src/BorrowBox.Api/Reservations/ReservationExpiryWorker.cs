using BorrowBox.Core.Reservations;

namespace BorrowBox.Api.Reservations;

public sealed class ReservationExpiryWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<ReservationExpiryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            using var scope = scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ReservationExpiryService>();
            var count = await service.ExpireDueAsync(stoppingToken);
            if (count > 0) logger.LogInformation("Expired {ReservationCount} reservations", count);
        }
    }
}
