using Chat.Application.Services;
using Chat.Domain;
using Chat.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Chat.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddChatInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IConversationRepository, ConversationRepository>();
        // Stub implementations for ports
        services.AddSingleton<ISpecGenerator, NoopSpecGenerator>();
        services.AddSingleton<IOpenApiDiff, NoopOpenApiDiff>();
        services.AddScoped<IChatAppService, ChatAppService>();
        return services;
    }
}

internal class NoopSpecGenerator : ISpecGenerator { public Task<string> GenerateSpecAsync(string prompt, CancellationToken ct) => Task.FromResult("{} /* spec */"); }
internal class NoopOpenApiDiff : IOpenApiDiff { public Task<string> DiffAsync(string oldSpec, string newSpec, CancellationToken ct) => Task.FromResult("{} /* diff */"); }
