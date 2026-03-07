using MediatR;
using TaskBoard.Domain.Enums;

namespace TaskBoard.Application.Commands.AddTaskToProject;

public record AddTaskToProjectCommand(
    Guid ProjectId,
    string Title,
    TaskPriority Priority
) : IRequest<Guid>;