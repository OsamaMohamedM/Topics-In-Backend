using TaskBoard.Domain.Entities;
using TaskBoard.Domain.Enums;
using TaskBoard.Domain.Exceptions;

namespace TaskBoard.Domain.Aggregates;

public class Project
{
    public const int MaxTasksPerProject = 20;

    private readonly List<TaskItem> _tasks = new();

    private Project()
    { }

    public Project(Guid id, string name, string description, string owner)
    {
        Id = id;
        Name = name;
        Description = description;
        Owner = owner;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Owner { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    public IReadOnlyCollection<TaskItem> Tasks => _tasks.AsReadOnly();

    public TaskItem AddTask(string title, TaskPriority priority)
    {
        // ── Enforce the 20-task limit ──
        if (_tasks.Count >= MaxTasksPerProject)
        {
            throw new DomainException(
                $"Project '{Name}' already has the maximum of {MaxTasksPerProject} tasks. " +
                "Remove existing tasks before adding new ones.");
        }

        var task = new TaskItem(
            id: Guid.NewGuid(),
            projectId: Id,
            title: title,
            priority: priority
        );

        _tasks.Add(task);
        return task;
    }
}