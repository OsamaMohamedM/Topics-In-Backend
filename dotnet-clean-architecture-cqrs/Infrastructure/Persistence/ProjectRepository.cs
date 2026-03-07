using Microsoft.EntityFrameworkCore;
using TaskBoard.Domain.Aggregates;
using TaskBoard.Domain.Interfaces;

namespace TaskBoard.Infrastructure.Persistence;

public class ProjectRepository : IProjectRepository
{
    private readonly AppDbContext _dbContext;

    public ProjectRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Projects.FindAsync(
            new object[] { id }, cancellationToken);
    }

    public async Task<Project?> GetByIdWithTasksAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Projects
            .Include(p => p.Tasks)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Project?> GetProjectByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Projects
            .Include(p => p.Tasks)
            .FirstOrDefaultAsync(
                p => p.Tasks.Any(t => t.Id == taskId),
                cancellationToken);
    }

    public async Task AddAsync(Project project, CancellationToken cancellationToken = default)
    {
        await _dbContext.Projects.AddAsync(project, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}