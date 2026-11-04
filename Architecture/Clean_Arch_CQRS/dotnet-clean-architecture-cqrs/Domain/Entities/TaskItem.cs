using TaskBoard.Domain.Enums;
using TaskBoard.Domain.Exceptions;

namespace TaskBoard.Domain.Entities;

public class TaskItem
{
    private TaskItem()
    { }

    internal TaskItem(Guid id, Guid projectId, string title, TaskPriority priority)
    {
        Id = id;
        ProjectId = projectId;
        Title = title;
        Priority = priority;
        Status = TaskItemStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid ProjectId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public TaskItemStatus Status { get; private set; }
    public TaskPriority Priority { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public void MarkAsCompleted()
    {
        if (Status == TaskItemStatus.Completed)
        {
            throw new DomainException($"Task '{Title}' is already completed and cannot be completed again.");
        }

        Status = TaskItemStatus.Completed;
    }
}