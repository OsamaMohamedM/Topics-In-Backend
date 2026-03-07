using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TaskBoard.Application.Behaviors;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        _logger.LogInformation("[START] Handling {RequestName}", requestName);

        var stopwatch = Stopwatch.StartNew();
        var response = await next();

        stopwatch.Stop();
        _logger.LogInformation(
            "[END] Handled {RequestName} in {ElapsedMs}ms",
            requestName,
            stopwatch.ElapsedMilliseconds);

        return response;
    }
}