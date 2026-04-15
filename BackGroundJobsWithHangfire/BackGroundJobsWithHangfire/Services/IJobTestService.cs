namespace BackGroundJobsWithHangfire.Services;

public interface IJobTestService
{
    void FireAndForgetTask(string message);

    void DelayedTask(string message);

    void RecurringTask(string message);
}