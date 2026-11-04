using FluentValidation;

namespace TaskBoard.Application.Commands.AddTaskToProject;

public class AddTaskToProjectCommandValidator : AbstractValidator<AddTaskToProjectCommand>
{
    public AddTaskToProjectCommandValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty()
                .WithMessage("ProjectId is required.");

        RuleFor(x => x.Title)
            .NotEmpty()
                .WithMessage("Task title is required.")
            .MaximumLength(200)
                .WithMessage("Task title must be 200 characters or fewer.");
        RuleFor(x => x.Priority)
            .IsInEnum()
                .WithMessage("Priority must be Low (0), Medium (1), or High (2).");
    }
}