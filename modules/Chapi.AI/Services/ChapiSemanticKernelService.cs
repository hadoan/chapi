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
    private readonly Chapi.AI.Utilities.RunPackBuilder _runPackBuilder;

    public ChapiSemanticKernelService(
        IConfiguration configuration,
        ILogger<SemanticKernelService> logger,
        Chapi.AI.Utilities.RunPackBuilder builder)
        : base(configuration, logger)
    {
        _runPackBuilder = builder;
    }

    // protected override void SetupToolPlugins(IKernelBuilder kernelBuilder)
    // {
    //     base.SetupToolPlugins(kernelBuilder);

    //     // Register RunPackPlugin as a tool plugin
    //     kernelBuilder.Plugins.AddFromObject(new Chapi.AI.Plugins.RunPack.RunPackPlugin(_runPackBuilder), "runpack");
    // }
}