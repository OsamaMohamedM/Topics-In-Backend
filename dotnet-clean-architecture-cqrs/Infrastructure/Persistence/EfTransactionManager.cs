using TaskBoard.Application.Behaviors;
using TaskBoard.Infrastructure.Persistence;

namespace dotnet_clean_architecture_cqrs.Infrastructure.Persistence
{
    public class EfTransactionManager : ITransactionManager
    {
        private readonly AppDbContext _dbContext;

        public EfTransactionManager(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_dbContext.Database.CurrentTransaction is null)
            {
                await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            }
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_dbContext.Database.CurrentTransaction is not null)
            {
                await _dbContext.Database.CommitTransactionAsync(cancellationToken);
            }
        }

        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_dbContext.Database.CurrentTransaction is not null)
            {
                await _dbContext.Database.RollbackTransactionAsync(cancellationToken);
            }
        }
    }
}