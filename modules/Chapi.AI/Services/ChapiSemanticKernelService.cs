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
    private readonly IServiceProvider _serviceProvider;

    public ChapiSemanticKernelService(
        IConfiguration configuration,
        ILogger<SemanticKernelService> logger,
        IServiceProvider serviceProvider)
        : base(configuration, logger)
    {
        _serviceProvider = serviceProvider;
    }

    protected override void SetupToolPlugins(IKernelBuilder kernelBuilder)
    {
        base.SetupToolPlugins(kernelBuilder);

        // Let DI create the plugin instance
        var runPackPlugin = _serviceProvider.GetRequiredService<Chapi.AI.Plugins.RunPack.RunPackPlugin>();
        
        // Register RunPackPlugin as a tool plugin
        kernelBuilder.Plugins.AddFromObject(runPackPlugin, "runpack_tools");
    }
}