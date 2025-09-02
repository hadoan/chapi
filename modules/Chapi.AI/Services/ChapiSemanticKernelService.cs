using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Text;
using OpenTelemetry.Trace;
using ShipMvp.Domain.Analytics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ShipMvp.Integration.SemanticKernel.Infrastructure;

namespace Chapi.AI.Services;

public class ChapiSemanticKernelService : SemanticKernelService
{
    public ChapiSemanticKernelService(
        IConfiguration configuration,
        ILogger<SemanticKernelService> logger,
        IServiceProvider serviceProvider)
        : base(configuration, logger, serviceProvider)
    {
    }

    protected override void SetupToolPlugins(Kernel kernel)
    {
        base.SetupToolPlugins(kernel);

        // Get RunPackPlugin from DI container if service provider is available
        if (_serviceProvider != null)
        {
            var runPackPlugin = _serviceProvider.GetRequiredService<Chapi.AI.Plugins.RunPack.RunPackPlugin>();
            kernel.Plugins.Add(Microsoft.SemanticKernel.KernelPluginFactory.CreateFromObject(runPackPlugin, "runpack_tools"));
        }

    }
}