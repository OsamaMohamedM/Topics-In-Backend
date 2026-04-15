using BackGroundJobsWithHangfire.Services;
using Hangfire;
using Microsoft.AspNetCore.Mvc;

namespace BackGroundJobsWithHangfire.Controllers;

[ApiController]
[Route("api/jobs")]
public class JobsController : ControllerBase
{
    [HttpPost("instant")]
    public IActionResult TriggerInstant([FromQuery] string message = "Immediate job triggered")
    {
        BackgroundJob.Enqueue<IJobTestService>(job => job.FireAndForgetTask(message));
        return Accepted(new { message = "Instant job queued", jobMessage = message });
    }

    [HttpPost("scheduled")]
    public IActionResult TriggerScheduled([FromQuery] string message = "Scheduled job triggered after 2 minutes")
    {
        BackgroundJob.Schedule<IJobTestService>(job => job.DelayedTask(message), TimeSpan.FromMinutes(2));
        return Accepted(new { message = "Scheduled job queued", jobMessage = message, delay = "00:02:00" });
    }
}