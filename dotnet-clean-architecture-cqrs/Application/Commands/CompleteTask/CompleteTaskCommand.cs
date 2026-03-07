using MediatR;

namespace TaskBoard.Application.Commands.CompleteTask;

public record CompleteTaskCommand(
    Guid TaskId
) : IRequest<Unit>;