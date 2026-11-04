using MediatR;
using TaskBoard.Domain.Aggregates;
using TaskBoard.Domain.Entities;
using TaskBoard.Domain.Interfaces;

namespace TaskBoard.Application.Commands.CompleteTask;

public class CompleteTaskCommandHandler : IRequestHandler<CompleteTaskCommand, Unit>
{
    private readonly IProjectRepository _projectRepository;

    public CompleteTaskCommandHandler(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public async Task<Unit> Handle(CompleteTaskCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdWithTasksAsync(request.TaskId, cancellationToken);
        var (foundProject, task) = await FindProjectAndTaskAsync(request.TaskId, cancellationToken);

        task.MarkAsCompleted();
        await _projectRepository.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }

    private async Task<(Project Project, TaskItem Task)> FindProjectAndTaskAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetProjectByTaskIdAsync(taskId, cancellationToken);

        if (project is null)
        {
            throw new KeyNotFoundException($"Task with ID '{taskId}' was not found.");
        }
        var task = project.Tasks.FirstOrDefault(t => t.Id == taskId);

        if (task is null)
        {
            throw new KeyNotFoundException($"Task with ID '{taskId}' was not found in the project.");
        }

        return (project, task);
    }
}