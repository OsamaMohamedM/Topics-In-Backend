using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TaskBoard.Application.Behaviors;

public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
    private const int ThresholdMs = 300;

    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = await next();

        stopwatch.Stop();
        if (stopwatch.ElapsedMilliseconds > ThresholdMs)
        {
            var requestName = typeof(TRequest).Name;
            _logger.LogWarning(
                "[PERFORMANCE] {RequestName} took {ElapsedMs}ms — exceeds threshold of {ThresholdMs}ms. " +
                "Consider investigating for performance issues (N+1 queries, missing indexes, etc.).",
                requestName,
                stopwatch.ElapsedMilliseconds,
                ThresholdMs);
        }

        return response;
    }
}