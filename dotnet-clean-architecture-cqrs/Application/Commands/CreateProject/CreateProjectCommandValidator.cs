using FluentValidation;

namespace TaskBoard.Application.Commands.CreateProject;

public class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
                .WithMessage("Project name is required.")
            .MaximumLength(100)
                .WithMessage("Project name must be 100 characters or fewer.");
        RuleFor(x => x.Owner)
            .NotEmpty()
                .WithMessage("Project owner is required.");
    }
}