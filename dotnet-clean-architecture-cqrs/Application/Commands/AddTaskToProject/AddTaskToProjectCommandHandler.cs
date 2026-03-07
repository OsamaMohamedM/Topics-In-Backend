using MediatR;
using TaskBoard.Domain.Interfaces;

namespace TaskBoard.Application.Commands.AddTaskToProject;

public class AddTaskToProjectCommandHandler : IRequestHandler<AddTaskToProjectCommand, Guid>
{
    private readonly IProjectRepository _projectRepository;

    public AddTaskToProjectCommandHandler(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public async Task<Guid> Handle(AddTaskToProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdWithTasksAsync(request.ProjectId, cancellationToken);

        if (project is null)
        {
            throw new KeyNotFoundException($"Project with ID '{request.ProjectId}' was not found.");
        }

        var task = project.AddTask(request.Title, request.Priority);
        await _projectRepository.SaveChangesAsync(cancellationToken);
        return task.Id;
    }
}