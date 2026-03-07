using MediatR;
using TaskBoard.Domain.Interfaces;

namespace TaskBoard.Application.Queries.GetProjectDetails;

public class GetProjectDetailsQueryHandler : IRequestHandler<GetProjectDetailsQuery, ProjectDetailsDto>
{
    private readonly IProjectRepository _projectRepository;

    public GetProjectDetailsQueryHandler(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public async Task<ProjectDetailsDto> Handle(
        GetProjectDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdWithTasksAsync(
            request.ProjectId, cancellationToken);
        if (project is null)
        {
            throw new KeyNotFoundException(
                $"Project with ID '{request.ProjectId}' was not found.");
        }
        var dto = new ProjectDetailsDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            Owner = project.Owner,
            CreatedAt = project.CreatedAt,
            TotalTasks = project.Tasks.Count,
            CompletedTasks = project.Tasks.Count(t =>
                t.Status == Domain.Enums.TaskItemStatus.Completed),
            Tasks = project.Tasks.Select(t => new TaskItemDto
            {
                Id = t.Id,
                Title = t.Title,
                Status = t.Status.ToString(),
                Priority = t.Priority.ToString(),
                CreatedAt = t.CreatedAt
            }).ToList()
        };

        return dto;
    }
}