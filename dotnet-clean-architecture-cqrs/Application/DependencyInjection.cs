// ── FILE: src/TaskBoard.Application/DependencyInjection.cs ───────────────────
//
// WHY a DependencyInjection.cs file per layer? ───────────────────────────────────────────── Each
// layer is responsible for registering ITS OWN services with the DI container. This keeps
// registration organized and prevents the API's Program.cs from becoming a giant list of registrations.
//
// The API's Program.cs simply calls: builder.Services.AddApplicationServices(); builder.Services.AddInfrastructureServices(connectionString);

using FluentValidation;
using MediatR;
using TaskBoard.Application.Behaviors;

namespace TaskBoard.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);

            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}