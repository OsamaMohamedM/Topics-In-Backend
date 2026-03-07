using MediatR;

namespace TaskBoard.Application.Queries.GetProjectDetails;

public record GetProjectDetailsQuery(
    Guid ProjectId
) : IRequest<ProjectDetailsDto>;