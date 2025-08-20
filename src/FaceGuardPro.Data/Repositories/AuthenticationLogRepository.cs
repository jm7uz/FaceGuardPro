using Microsoft.EntityFrameworkCore;
using FaceGuardPro.Data.Context;
using FaceGuardPro.Data.Entities;

namespace FaceGuardPro.Data.Repositories;

public class AuthenticationLogRepository : BaseRepository<AuthenticationLog>, IAuthenticationLogRepository
{
    public AuthenticationLogRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<AuthenticationLog>> GetByEmployeeIdAsync(Guid employeeId)
    {
        return await _dbSet
            .Include(a => a.Employee)
            .Where(a => a.EmployeeId == employeeId)
            .OrderByDescending(a => a.AttemptedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuthenticationLog>> GetRecentAttemptsAsync(Guid employeeId, TimeSpan timeSpan)
    {
        var cutoffTime = DateTime.UtcNow.Subtract(timeSpan);

        return await _dbSet
            .Include(a => a.Employee)
            .Where(a => a.EmployeeId == employeeId && a.AttemptedAt >= cutoffTime)
            .OrderByDescending(a => a.AttemptedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuthenticationLog>> GetFailedAttemptsAsync(Guid employeeId, TimeSpan timeSpan)
    {
        var cutoffTime = DateTime.UtcNow.Subtract(timeSpan);

        return await _dbSet
            .Include(a => a.Employee)
            .Where(a => a.EmployeeId == employeeId &&
                       a.AttemptedAt >= cutoffTime &&
                       a.AuthenticationResult != "Success")
            .OrderByDescending(a => a.AttemptedAt)
            .ToListAsync();
    }

    public async Task<int> GetFailedAttemptCountAsync(Guid employeeId, TimeSpan timeSpan)
    {
        var cutoffTime = DateTime.UtcNow.Subtract(timeSpan);

        return await _dbSet
            .CountAsync(a => a.EmployeeId == employeeId &&
                           a.AttemptedAt >= cutoffTime &&
                           a.AuthenticationResult != "Success");
    }

    public async Task<IEnumerable<AuthenticationLog>> GetAuthenticationLogsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Include(a => a.Employee)
            .Where(a => a.AttemptedAt >= startDate && a.AttemptedAt <= endDate)
            .OrderByDescending(a => a.AttemptedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuthenticationLog>> GetSuccessfulAuthenticationsAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Include(a => a.Employee)
            .Where(a => a.AttemptedAt >= startDate &&
                       a.AttemptedAt <= endDate &&
                       a.AuthenticationResult == "Success")
            .OrderByDescending(a => a.AttemptedAt)
            .ToListAsync();
    }

    public async Task<Dictionary<string, int>> GetAuthenticationStatsByResultAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Where(a => a.AttemptedAt >= startDate && a.AttemptedAt <= endDate)
            .GroupBy(a => a.AuthenticationResult)
            .Select(g => new { Result = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Result, x => x.Count);
    }

    public override async Task<IEnumerable<AuthenticationLog>> GetAllAsync()
    {
        return await _dbSet
            .Include(a => a.Employee)
            .OrderByDescending(a => a.AttemptedAt)
            .ToListAsync();
    }

    public override async Task<IEnumerable<AuthenticationLog>> GetPagedAsync(int pageNumber, int pageSize)
    {
        return await _dbSet
            .Include(a => a.Employee)
            .OrderByDescending(a => a.AttemptedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
}