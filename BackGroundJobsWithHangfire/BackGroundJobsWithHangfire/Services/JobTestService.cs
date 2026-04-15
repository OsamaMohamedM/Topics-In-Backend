namespace BackGroundJobsWithHangfire.Services;

public class JobTestService : IJobTestService
{
    public void FireAndForgetTask(string message)
    {
        Console.WriteLine($"[FireAndForget] {message}");
    }

    public void DelayedTask(string message)
    {
        Console.WriteLine($"[Delayed] {message}");
    }

    public void RecurringTask(string message)
    {
        Console.WriteLine($"[Recurring] {message}");
    }
}