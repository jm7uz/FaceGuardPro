using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using FaceGuardPro.Data.Context;
using FaceGuardPro.Shared.Models;

namespace FaceGuardPro.API.Controllers;

/// <summary>
/// Health check endpoints
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class HealthController : BaseController
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        AppDbContext dbContext,
        ILogger<HealthController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Basic health check
    /// </summary>
    /// <returns>API health status</returns>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<HealthCheckResult>), 200)]
    public async Task<ActionResult<ApiResponse<HealthCheckResult>>> GetHealth()
    {
        try
        {
            var healthResult = new HealthCheckResult
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                ApiVersion = GetApiVersion(),
                Environment = GetEnvironment(),
                Uptime = GetUptime()
            };

            return Success(healthResult, "API is healthy");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            var healthResult = new HealthCheckResult
            {
                Status = "Unhealthy",
                Timestamp = DateTime.UtcNow,
                ApiVersion = GetApiVersion(),
                Environment = GetEnvironment(),
                Error = ex.Message
            };

            return InternalServerError<HealthCheckResult>("Health check failed");
        }
    }

    /// <summary>
    /// Detailed health check including database
    /// </summary>
    /// <returns>Detailed health status</returns>
    [HttpGet("detailed")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<DetailedHealthCheckResult>), 200)]
    public async Task<ActionResult<ApiResponse<DetailedHealthCheckResult>>> GetDetailedHealth()
    {
        var healthResult = new DetailedHealthCheckResult
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            ApiVersion = GetApiVersion(),
            Environment = GetEnvironment(),
            Uptime = GetUptime(),
            Checks = new List<HealthCheckItem>()
        };

        try
        {
            // Database health check
            var dbCheckResult = await CheckDatabaseHealth();
            healthResult.Checks.Add(dbCheckResult);

            // Memory health check
            var memoryCheckResult = CheckMemoryHealth();
            healthResult.Checks.Add(memoryCheckResult);

            // Disk space health check
            var diskCheckResult = CheckDiskHealth();
            healthResult.Checks.Add(diskCheckResult);

            // Overall status
            var hasUnhealthyChecks = healthResult.Checks.Any(c => c.Status != "Healthy");
            if (hasUnhealthyChecks)
            {
                healthResult.Status = "Degraded";
            }

            return Success(healthResult, "Detailed health check completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Detailed health check failed");
            healthResult.Status = "Unhealthy";
            healthResult.Error = ex.Message;

            return InternalServerError<DetailedHealthCheckResult>("Detailed health check failed");
        }
    }

    /// <summary>
    /// Database connectivity check
    /// </summary>
    /// <returns>Database health status</returns>
    [HttpGet("database")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<HealthCheckItem>), 200)]
    public async Task<ActionResult<ApiResponse<HealthCheckItem>>> GetDatabaseHealth()
    {
        try
        {
            var dbCheckResult = await CheckDatabaseHealth();
            return Success(dbCheckResult, "Database health check completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            var dbCheckResult = new HealthCheckItem
            {
                Name = "Database",
                Status = "Unhealthy",
                ResponseTime = TimeSpan.Zero,
                Error = ex.Message,
                CheckedAt = DateTime.UtcNow
            };

            return Success(dbCheckResult, "Database health check failed");
        }
    }

    private async Task<HealthCheckItem> CheckDatabaseHealth()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Simple database connectivity test
            await _dbContext.Database.OpenConnectionAsync();
            await _dbContext.Database.CloseConnectionAsync();

            // Test query
            var userCount = await _dbContext.Users.CountAsync();

            stopwatch.Stop();

            return new HealthCheckItem
            {
                Name = "Database",
                Status = "Healthy",
                ResponseTime = stopwatch.Elapsed,
                Details = new Dictionary<string, object>
                {
                    ["ConnectionString"] = _dbContext.Database.GetConnectionString()?.Split(';')[0] + ";...",
                    ["UserCount"] = userCount,
                    ["DatabaseProvider"] = _dbContext.Database.ProviderName
                },
                CheckedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            return new HealthCheckItem
            {
                Name = "Database",
                Status = "Unhealthy",
                ResponseTime = stopwatch.Elapsed,
                Error = ex.Message,
                CheckedAt = DateTime.UtcNow
            };
        }
    }

    private HealthCheckItem CheckMemoryHealth()
    {
        try
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            var workingSet = process.WorkingSet64;
            var privateMemory = process.PrivateMemorySize64;

            // Convert to MB
            var workingSetMB = workingSet / (1024 * 1024);
            var privateMemoryMB = privateMemory / (1024 * 1024);

            // Simple threshold check (adjust as needed)
            var status = workingSetMB > 1000 ? "Warning" : "Healthy";

            return new HealthCheckItem
            {
                Name = "Memory",
                Status = status,
                Details = new Dictionary<string, object>
                {
                    ["WorkingSetMB"] = workingSetMB,
                    ["PrivateMemoryMB"] = privateMemoryMB,
                    ["GCTotalMemory"] = GC.GetTotalMemory(false) / (1024 * 1024)
                },
                CheckedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            return new HealthCheckItem
            {
                Name = "Memory",
                Status = "Unhealthy",
                Error = ex.Message,
                CheckedAt = DateTime.UtcNow
            };
        }
    }

    private HealthCheckItem CheckDiskHealth()
    {
        try
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var drive = new DriveInfo(Path.GetPathRoot(currentDirectory)!);

            var totalSpaceGB = drive.TotalSize / (1024 * 1024 * 1024);
            var freeSpaceGB = drive.AvailableFreeSpace / (1024 * 1024 * 1024);
            var usedSpaceGB = totalSpaceGB - freeSpaceGB;
            var usagePercentage = (double)usedSpaceGB / totalSpaceGB * 100;

            // Simple threshold check
            var status = usagePercentage > 90 ? "Warning" : "Healthy";
            if (usagePercentage > 95) status = "Unhealthy";

            return new HealthCheckItem
            {
                Name = "Disk",
                Status = status,
                Details = new Dictionary<string, object>
                {
                    ["TotalSpaceGB"] = totalSpaceGB,
                    ["FreeSpaceGB"] = freeSpaceGB,
                    ["UsedSpaceGB"] = usedSpaceGB,
                    ["UsagePercentage"] = Math.Round(usagePercentage, 2),
                    ["DriveName"] = drive.Name
                },
                CheckedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            return new HealthCheckItem
            {
                Name = "Disk",
                Status = "Unhealthy",
                Error = ex.Message,
                CheckedAt = DateTime.UtcNow
            };
        }
    }

    private string GetApiVersion()
    {
        return typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0";
    }

    private string GetEnvironment()
    {
        return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown";
    }

    private TimeSpan GetUptime()
    {
        return DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime();
    }
}

// Health check result models
public class HealthCheckResult
{
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string ApiVersion { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public TimeSpan Uptime { get; set; }
    public string? Error { get; set; }
}

public class DetailedHealthCheckResult : HealthCheckResult
{
    public List<HealthCheckItem> Checks { get; set; } = new();
}

public class HealthCheckItem
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public TimeSpan ResponseTime { get; set; }
    public Dictionary<string, object>? Details { get; set; }
    public string? Error { get; set; }
    public DateTime CheckedAt { get; set; }
}