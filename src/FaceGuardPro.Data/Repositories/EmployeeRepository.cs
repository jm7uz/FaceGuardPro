using Microsoft.EntityFrameworkCore;
using FaceGuardPro.Data.Context;
using FaceGuardPro.Data.Entities;
using FaceGuardPro.Shared.Enums;

namespace FaceGuardPro.Data.Repositories;

public class EmployeeRepository : BaseRepository<Employee>, IEmployeeRepository
{
    public EmployeeRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Employee?> GetByEmployeeIdAsync(string employeeId)
    {
        return await _dbSet
            .Include(e => e.FaceTemplates)
            .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);
    }

    public async Task<Employee?> GetByEmailAsync(string email)
    {
        return await _dbSet
            .Include(e => e.FaceTemplates)
            .FirstOrDefaultAsync(e => e.Email == email);
    }

    public async Task<IEnumerable<Employee>> GetByDepartmentAsync(string department)
    {
        return await _dbSet
            .Include(e => e.FaceTemplates)
            .Where(e => e.Department == department && e.Status == EmployeeStatus.Active)
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .ToListAsync();
    }

    public async Task<IEnumerable<Employee>> GetActiveEmployeesAsync()
    {
        return await _dbSet
            .Include(e => e.FaceTemplates)
            .Where(e => e.Status == EmployeeStatus.Active)
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .ToListAsync();
    }

    public async Task<IEnumerable<Employee>> SearchEmployeesAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return await GetActiveEmployeesAsync();

        var lowerSearchTerm = searchTerm.ToLower();

        return await _dbSet
            .Include(e => e.FaceTemplates)
            .Where(e =>
                (e.FirstName.ToLower().Contains(lowerSearchTerm) ||
                 e.LastName.ToLower().Contains(lowerSearchTerm) ||
                 e.EmployeeId.ToLower().Contains(lowerSearchTerm) ||
                 e.Email.ToLower().Contains(lowerSearchTerm) ||
                 e.Department.ToLower().Contains(lowerSearchTerm) ||
                 e.Position.ToLower().Contains(lowerSearchTerm)) &&
                 e.Status == EmployeeStatus.Active)
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .ToListAsync();
    }

    public async Task<bool> IsEmployeeIdUniqueAsync(string employeeId, Guid? excludeId = null)
    {
        var query = _dbSet.Where(e => e.EmployeeId == employeeId);

        if (excludeId.HasValue)
        {
            query = query.Where(e => e.Id != excludeId.Value);
        }

        return !await query.AnyAsync();
    }

    public async Task<bool> IsEmailUniqueAsync(string email, Guid? excludeId = null)
    {
        var query = _dbSet.Where(e => e.Email == email);

        if (excludeId.HasValue)
        {
            query = query.Where(e => e.Id != excludeId.Value);
        }

        return !await query.AnyAsync();
    }

    public async Task<IEnumerable<Employee>> GetEmployeesWithFaceTemplatesAsync()
    {
        return await _dbSet
            .Include(e => e.FaceTemplates)
            .Where(e => e.FaceTemplates.Any() && e.Status == EmployeeStatus.Active)
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .ToListAsync();
    }

    public override async Task<Employee?> GetByIdAsync(Guid id)
    {
        return await _dbSet
            .Include(e => e.FaceTemplates)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public override async Task<IEnumerable<Employee>> GetAllAsync()
    {
        return await _dbSet
            .Include(e => e.FaceTemplates)
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .ToListAsync();
    }

    public override async Task<IEnumerable<Employee>> GetPagedAsync(int pageNumber, int pageSize)
    {
        return await _dbSet
            .Include(e => e.FaceTemplates)
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
}