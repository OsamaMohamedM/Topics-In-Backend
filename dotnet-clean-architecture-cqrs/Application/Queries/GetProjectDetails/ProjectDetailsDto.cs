namespace TaskBoard.Application.Queries.GetProjectDetails;

public record ProjectDetailsDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Owner { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public int TotalTasks { get; init; }
    public int CompletedTasks { get; init; }
    public List<TaskItemDto> Tasks { get; init; } = new();
}

public record TaskItemDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}