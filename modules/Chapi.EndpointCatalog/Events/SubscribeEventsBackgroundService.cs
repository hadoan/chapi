using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using ShipMvp.Core.EventBus;
using Chapi.SharedKernel.Events;

namespace Chapi.EndpointCatalog.Events;

public class SubscribeEventsBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDistributedEventBus _eventBus;
    private readonly ILogger<SubscribeEventsBackgroundService> _logger;

    public SubscribeEventsBackgroundService(IServiceProvider serviceProvider, IDistributedEventBus eventBus, ILogger<SubscribeEventsBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _eventBus = eventBus;
        _logger = logger;
        _logger.LogInformation("SubscribeEventsBackgroundService constructed");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Subscribing BuildCatalogOnSpecImportedHandler to ApiSpecImportedEto");
        try
        {
            await _eventBus.SubscribeAsync<ApiSpecImportedEto, BuildCatalogOnSpecImportedHandler>();
            _logger.LogInformation("Subscription complete");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe BuildCatalogOnSpecImportedHandler");
        }

        // Keep the background service alive until shutdown
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (TaskCanceledException) { }
    }
}
