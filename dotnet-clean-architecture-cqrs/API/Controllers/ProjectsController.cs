using MediatR;
using Microsoft.AspNetCore.Mvc;
using TaskBoard.Application.Commands.CreateProject;
using TaskBoard.Application.Queries.GetProjectDetails;

namespace TaskBoard.API.Controllers;

[ApiController]
[Route("api/[controller]/v1.0")]
public class ProjectsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProjectsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequest request)
    {
        var command = new CreateProjectCommand(request.Name, request.Description, request.Owner);
        var projectId = await _mediator.Send(command);

        return CreatedAtAction(
            nameof(GetProjectDetails),
            new { id = projectId },
            new { projectId });
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProjectDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProjectDetails(Guid id)
    {
        var query = new GetProjectDetailsQuery(id);
        var result = await _mediator.Send(query);

        return Ok(result);
    }
}

public record CreateProjectRequest(
    string Name,
    string Description,
    string Owner
);