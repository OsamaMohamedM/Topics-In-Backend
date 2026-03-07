using MediatR;
using TaskBoard.Domain.Aggregates;
using TaskBoard.Domain.Interfaces;

namespace TaskBoard.Application.Commands.CreateProject;

public class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, Guid>
{
    private readonly IProjectRepository _projectRepository;

    public CreateProjectCommandHandler(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public async Task<Guid> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        var project = new Project(
            id: Guid.NewGuid(),
            name: request.Name,
            description: request.Description,
            owner: request.Owner
        );

        await _projectRepository.AddAsync(project, cancellationToken);
        await _projectRepository.SaveChangesAsync(cancellationToken);
        return project.Id;
    }
}