using System.Linq.Expressions;

namespace FaceGuardPro.Data.Repositories;

public interface IRepository<T> where T : class
{
    // Get operations
    Task<T?> GetByIdAsync(Guid id);
    Task<T?> GetFirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> GetWhereAsync(Expression<Func<T, bool>> predicate);
    Task<IEnumerable<T>> GetPagedAsync(int pageNumber, int pageSize);
    Task<IEnumerable<T>> GetPagedAsync(Expression<Func<T, bool>> predicate, int pageNumber, int pageSize);

    // Count operations
    Task<int> CountAsync();
    Task<int> CountAsync(Expression<Func<T, bool>> predicate);

    // Existence check
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);

    // Add operations
    Task<T> AddAsync(T entity);
    Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities);

    // Update operations
    Task<T> UpdateAsync(T entity);
    Task<IEnumerable<T>> UpdateRangeAsync(IEnumerable<T> entities);

    // Delete operations
    Task<bool> DeleteAsync(Guid id);
    Task<bool> DeleteAsync(T entity);
    Task<int> DeleteRangeAsync(IEnumerable<T> entities);
    Task<int> DeleteWhereAsync(Expression<Func<T, bool>> predicate);

    // Include operations for navigation properties
    IQueryable<T> Include<TProperty>(Expression<Func<T, TProperty>> navigationPropertyPath);
    IQueryable<T> IncludeMultiple(params Expression<Func<T, object>>[] includeProperties);

    // Raw query operations
    Task<IEnumerable<T>> FromSqlRawAsync(string sql, params object[] parameters);

    // Transaction support
    Task<int> SaveChangesAsync();
}

public interface IEmployeeRepository : IRepository<Entities.Employee>
{
    Task<Entities.Employee?> GetByEmployeeIdAsync(string employeeId);
    Task<Entities.Employee?> GetByEmailAsync(string email);
    Task<IEnumerable<Entities.Employee>> GetByDepartmentAsync(string department);
    Task<IEnumerable<Entities.Employee>> GetActiveEmployeesAsync();
    Task<IEnumerable<Entities.Employee>> SearchEmployeesAsync(string searchTerm);
    Task<bool> IsEmployeeIdUniqueAsync(string employeeId, Guid? excludeId = null);
    Task<bool> IsEmailUniqueAsync(string email, Guid? excludeId = null);
    Task<IEnumerable<Entities.Employee>> GetEmployeesWithFaceTemplatesAsync();
}

public interface IFaceTemplateRepository : IRepository<Entities.FaceTemplate>
{
    Task<Entities.FaceTemplate?> GetByEmployeeIdAsync(Guid employeeId);
    Task<IEnumerable<Entities.FaceTemplate>> GetByEmployeeIdsAsync(IEnumerable<Guid> employeeIds);
    Task<bool> HasFaceTemplateAsync(Guid employeeId);
    Task<int> DeleteByEmployeeIdAsync(Guid employeeId);
    Task<IEnumerable<Entities.FaceTemplate>> GetHighQualityTemplatesAsync(double minQuality = 0.8);
}

public interface IUserRepository : IRepository<Entities.User>
{
    Task<Entities.User?> GetByUsernameAsync(string username);
    Task<Entities.User?> GetByEmailAsync(string email);
    Task<Entities.User?> GetUserWithRolesAsync(Guid userId);
    Task<Entities.User?> GetUserWithRolesAsync(string username);
    Task<IEnumerable<Entities.User>> GetActiveUsersAsync();
    Task<IEnumerable<string>> GetUserRolesAsync(Guid userId);
    Task<IEnumerable<string>> GetUserPermissionsAsync(Guid userId);
    Task<bool> IsUsernameUniqueAsync(string username, Guid? excludeId = null);
    Task<bool> IsEmailUniqueAsync(string email, Guid? excludeId = null);
}

public interface IRoleRepository : IRepository<Entities.Role>
{
    Task<Entities.Role?> GetByNameAsync(string name);
    Task<IEnumerable<Entities.Role>> GetRolesWithPermissionsAsync();
    Task<IEnumerable<Entities.Permission>> GetRolePermissionsAsync(Guid roleId);
    Task<bool> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId);
    Task<bool> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId);
}

public interface IPermissionRepository : IRepository<Entities.Permission>
{
    Task<Entities.Permission?> GetByNameAsync(string name);
    Task<IEnumerable<Entities.Permission>> GetUnassignedPermissionsAsync(Guid roleId);
    Task<IEnumerable<Entities.Permission>> GetPermissionsByRoleAsync(Guid roleId);
}

public interface IAuthenticationLogRepository : IRepository<Entities.AuthenticationLog>
{
    Task<IEnumerable<Entities.AuthenticationLog>> GetByEmployeeIdAsync(Guid employeeId);
    Task<IEnumerable<Entities.AuthenticationLog>> GetRecentAttemptsAsync(Guid employeeId, TimeSpan timeSpan);
    Task<IEnumerable<Entities.AuthenticationLog>> GetFailedAttemptsAsync(Guid employeeId, TimeSpan timeSpan);
    Task<int> GetFailedAttemptCountAsync(Guid employeeId, TimeSpan timeSpan);
    Task<IEnumerable<Entities.AuthenticationLog>> GetAuthenticationLogsByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<Entities.AuthenticationLog>> GetSuccessfulAuthenticationsAsync(DateTime startDate, DateTime endDate);
    Task<Dictionary<string, int>> GetAuthenticationStatsByResultAsync(DateTime startDate, DateTime endDate);
}

public interface IRefreshTokenRepository : IRepository<Entities.RefreshToken>
{
    Task<Entities.RefreshToken?> GetByTokenAsync(string token);
    Task<IEnumerable<Entities.RefreshToken>> GetActiveTokensByUserAsync(Guid userId);
    Task<int> RevokeAllUserTokensAsync(Guid userId);
    Task<int> RevokeExpiredTokensAsync();
    Task<bool> IsTokenActiveAsync(string token);
}