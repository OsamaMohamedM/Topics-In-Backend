using dotnet_clean_architecture_cqrs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Behaviors;
using TaskBoard.Domain.Interfaces;
using TaskBoard.Infrastructure.Persistence;

namespace TaskBoard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));
        services.AddScoped<IProjectRepository, ProjectRepository>();

        services.AddScoped<ITransactionManager, EfTransactionManager>();

        return services;
    }
}