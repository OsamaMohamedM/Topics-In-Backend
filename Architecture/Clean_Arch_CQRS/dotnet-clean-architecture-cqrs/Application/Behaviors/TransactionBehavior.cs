using MediatR;
using Microsoft.Extensions.Logging;

namespace TaskBoard.Application.Behaviors;

public interface ITransactionManager
{
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}

public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ITransactionManager _transactionManager;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public TransactionBehavior(
        ITransactionManager transactionManager,
        ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _transactionManager = transactionManager;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        if (!requestName.EndsWith("Command"))
        {
            return await next();
        }

        _logger.LogInformation("[TRANSACTION] Starting transaction for {RequestName}", requestName);

        try
        {
            await _transactionManager.BeginTransactionAsync(cancellationToken);

            var response = await next();
            await _transactionManager.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("[TRANSACTION] Committed transaction for {RequestName}", requestName);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[TRANSACTION] Rolling back transaction for {RequestName} due to: {Message}",
                requestName,
                ex.Message);

            await _transactionManager.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}