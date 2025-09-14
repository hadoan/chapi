using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using ShipMvp.Core.EventBus;
using Runs.Application.Contracts.Events;
using Runs.Application.Handlers;

namespace Runs.Application.Services;

public class SubscribeEventsBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDistributedEventBus _eventBus;
    private readonly ILogger<SubscribeEventsBackgroundService> _logger;

    public SubscribeEventsBackgroundService(
        IServiceProvider serviceProvider,
        IDistributedEventBus eventBus,
        ILogger<SubscribeEventsBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _eventBus = eventBus;
        _logger = logger;
        _logger.LogInformation("SubscribeEventsBackgroundService constructed");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Subscribing RunRequestedHandler to RunRequestedEto");
        try
        {
            await _eventBus.SubscribeAsync<RunRequestedEto, RunRequestedHandler>();
            _logger.LogInformation("Subscription complete");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe RunRequestedHandler");
        }

        // Keep the background service alive until shutdown
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (TaskCanceledException) { }
    }
}