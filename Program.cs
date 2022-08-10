using Hangfire;
using Hangfire.Server;
using Hangfire.SqlServer;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddHangfire(configuration => configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(builder.Configuration.GetConnectionString("HangfireConnection"),
            new SqlServerStorageOptions
            {
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.Zero,
                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks = true
            }))
    .AddHangfireServer()
    .AddTransient<Job>();

var app = builder.Build();

app.UseHangfireDashboard();

app.MapGet("/", () => "Hello World!");
app.MapGet("/schedule", (IBackgroundJobClient backgroundJobs) => backgroundJobs.Enqueue<Job>(_ => _.Execute(null!, CancellationToken.None)));
app.MapGet("/cancel/{jobId}", (string jobId, IBackgroundJobClient backgroundJobs) => backgroundJobs.Delete(jobId));

app.Run();

public class Job
{
    private readonly ILogger<Job> _logger;

    public Job(ILogger<Job> logger)
    {
        _logger = logger;
    }

    public async Task Execute(PerformContext context, CancellationToken cancellationToken)
    {
        try
        {
            for (var i = 0; i < 15; i++)
            {
                _logger.LogInformation("Running job {JobId} for {Seconds}s", context.BackgroundJob.Id, i);
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
        catch (TaskCanceledException e)
        {
            _logger.LogWarning(e, "job was cancelled");
        }
    }
}
