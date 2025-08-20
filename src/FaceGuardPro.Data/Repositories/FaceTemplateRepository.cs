using Microsoft.EntityFrameworkCore;
using FaceGuardPro.Data.Context;
using FaceGuardPro.Data.Entities;

namespace FaceGuardPro.Data.Repositories;

public class FaceTemplateRepository : BaseRepository<FaceTemplate>, IFaceTemplateRepository
{
    public FaceTemplateRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<FaceTemplate?> GetByEmployeeIdAsync(Guid employeeId)
    {
        return await _dbSet
            .Include(f => f.Employee)
            .FirstOrDefaultAsync(f => f.EmployeeId == employeeId);
    }

    public async Task<IEnumerable<FaceTemplate>> GetByEmployeeIdsAsync(IEnumerable<Guid> employeeIds)
    {
        return await _dbSet
            .Include(f => f.Employee)
            .Where(f => employeeIds.Contains(f.EmployeeId))
            .ToListAsync();
    }

    public async Task<bool> HasFaceTemplateAsync(Guid employeeId)
    {
        return await _dbSet.AnyAsync(f => f.EmployeeId == employeeId);
    }

    public async Task<int> DeleteByEmployeeIdAsync(Guid employeeId)
    {
        var templates = await _dbSet.Where(f => f.EmployeeId == employeeId).ToListAsync();
        _dbSet.RemoveRange(templates);
        return templates.Count;
    }

    public async Task<IEnumerable<FaceTemplate>> GetHighQualityTemplatesAsync(double minQuality = 0.8)
    {
        return await _dbSet
            .Include(f => f.Employee)
            .Where(f => f.Quality >= minQuality)
            .OrderByDescending(f => f.Quality)
            .ToListAsync();
    }

    public override async Task<FaceTemplate?> GetByIdAsync(Guid id)
    {
        return await _dbSet
            .Include(f => f.Employee)
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    public override async Task<IEnumerable<FaceTemplate>> GetAllAsync()
    {
        return await _dbSet
            .Include(f => f.Employee)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
    }

    public override async Task<IEnumerable<FaceTemplate>> GetPagedAsync(int pageNumber, int pageSize)
    {
        return await _dbSet
            .Include(f => f.Employee)
            .OrderByDescending(f => f.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
}