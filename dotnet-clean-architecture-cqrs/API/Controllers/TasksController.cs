// ── FILE: src/TaskBoard.API/Controllers/TasksController.cs ───────────────────
//
// Manages TaskItem operations (add task to project, complete task).
// Same thin-controller principle as ProjectsController.
//
// WHY separate controllers for Projects and Tasks?
// ─────────────────────────────────────────────────
// Following REST conventions:
//   - ProjectsController handles /api/projects routes
//   - TasksController handles /api/projects/{id}/tasks and /api/tasks routes
//
// This keeps each controller focused and follows the Single Responsibility
// Principle. Each controller handles one type of resource.
// ─────────────────────────────────────────────────────────────────────────────

using MediatR;
using Microsoft.AspNetCore.Mvc;
using TaskBoard.Application.Commands.AddTaskToProject;
using TaskBoard.Application.Commands.CompleteTask;
using TaskBoard.Domain.Enums;

namespace TaskBoard.API.Controllers;

/// <summary>
/// API controller for TaskItem operations.
/// </summary>
[ApiController]
[Route("api")]
public class TasksController : ControllerBase
{
    private readonly IMediator _mediator;

    public TasksController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Adds a new task to an existing project.
    /// POST /api/projects/{projectId}/tasks
    /// </summary>
    /// <remarks>
    /// The projectId comes from the URL route, and the task details from the body.
    /// The handler will:
    ///   1. Load the project aggregate (with existing tasks)
    ///   2. Call project.AddTask() which enforces the 20-task limit
    ///   3. Save the new task to the database
    /// 
    /// If the project has 20 tasks already → DomainException → HTTP 422
    /// If the projectId doesn't exist → KeyNotFoundException → HTTP 404
    /// </remarks>
    [HttpPost("projects/{projectId:guid}/tasks")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AddTaskToProject(
        Guid projectId,
        [FromBody] AddTaskRequest request)
    {
        var command = new AddTaskToProjectCommand(projectId, request.Title, request.Priority);
        var taskId = await _mediator.Send(command);

        return StatusCode(StatusCodes.Status201Created, new { taskId });
    }

    /// <summary>
    /// Marks a task as completed.
    /// PUT /api/tasks/{id}/complete
    /// </summary>
    /// <remarks>
    /// WHY PUT instead of PATCH?
    /// PUT is used because we're replacing the task's status entirely
    /// (setting it to Completed). In a more complex API, you might use
    /// PATCH for partial updates.
    /// 
    /// If the task is already completed → DomainException → HTTP 422
    /// If the task doesn't exist → KeyNotFoundException → HTTP 404
    /// </remarks>
    [HttpPut("tasks/{id:guid}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CompleteTask(Guid id)
    {
        var command = new CompleteTaskCommand(id);
        await _mediator.Send(command);

        // ── HTTP 204 No Content — operation succeeded, nothing to return ──
        return NoContent();
    }
}

/// <summary>
/// Request body for adding a task to a project.
/// </summary>
public record AddTaskRequest(
    string Title,
    TaskPriority Priority
);
