using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using ShipMvp.Core.Modules;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace ShipMvp.CLI
{
    // Minimal command resolver interface used by Program.cs
    public interface ICommandResolver
    {
        Task<bool> ExecuteCommandAsync(string commandName, string[] args);
    }

    // Simple implementation that supports a few known commands as no-ops
    internal class CommandResolver : ICommandResolver
    {
        private readonly ILogger<CommandResolver> _logger;
        public CommandResolver(ILogger<CommandResolver> logger) => _logger = logger;

        public Task<bool> ExecuteCommandAsync(string commandName, string[] args)
        {
            _logger.LogInformation("Executing CLI command: {Command}", commandName);

            // Simple built-in commands
            switch (commandName)
            {
                case "help":
                    Console.WriteLine("Help: no-op CLI in this build");
                    return Task.FromResult(true);
                case "seed-data":
                case "seed-integrations":
                case "run-sql":
                    Console.WriteLine($"Simulating '{commandName}' (no-op)");
                    return Task.FromResult(true);
                default:
                    Console.WriteLine($"Unknown command: {commandName}");
                    return Task.FromResult(false);
            }
        }
    }

    // Minimal module to register the command resolver
    public class CLIModule : IModule
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ICommandResolver, CommandResolver>();
        }

        public void Configure(IApplicationBuilder app, IHostEnvironment env)
        {
            // no-op
        }
    }
}
