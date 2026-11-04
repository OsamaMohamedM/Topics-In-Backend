using MediatR;

namespace TaskBoard.Application.Commands.CreateProject;

public record CreateProjectCommand(
    string Name,
    string Description,
    string Owner
) : IRequest<Guid>;