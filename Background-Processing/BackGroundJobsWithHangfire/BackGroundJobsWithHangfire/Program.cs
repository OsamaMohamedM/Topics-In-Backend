using BackGroundJobsWithHangfire.Services;
using Hangfire;
using Hangfire.MemoryStorage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddTransient<IJobTestService, JobTestService>();
builder.Services.AddHangfire(configuration => configuration.UseMemoryStorage());
builder.Services.AddHangfireServer();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.UseHangfireDashboard("/dashboared");

RecurringJob.AddOrUpdate<IJobTestService>(
    "system-heartbeat",
    job => job.RecurringTask("System heartbeat"),
    Cron.Minutely);

app.Run();