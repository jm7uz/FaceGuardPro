using Microsoft.EntityFrameworkCore;
using FaceGuardPro.Data.Context;
using FaceGuardPro.Data.Entities;

namespace FaceGuardPro.Data.Repositories;

public class RoleRepository : BaseRepository<Role>, IRoleRepository
{
    public RoleRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Role?> GetByNameAsync(string name)
    {
        return await _dbSet
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Name == name);
    }

    public async Task<IEnumerable<Role>> GetRolesWithPermissionsAsync()
    {
        return await _dbSet
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .OrderBy(r => r.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Permission>> GetRolePermissionsAsync(Guid roleId)
    {
        return await _context.RolePermissions
            .Include(rp => rp.Permission)
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.Permission)
            .ToListAsync();
    }

    public async Task<bool> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId)
    {
        var existingAssignment = await _context.RolePermissions
            .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

        if (existingAssignment != null)
            return false; // Already assigned

        var rolePermission = new RolePermission
        {
            RoleId = roleId,
            PermissionId = permissionId,
            AssignedAt = DateTime.UtcNow
        };

        await _context.RolePermissions.AddAsync(rolePermission);
        return true;
    }

    public async Task<bool> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId)
    {
        var rolePermission = await _context.RolePermissions
            .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

        if (rolePermission == null)
            return false;

        _context.RolePermissions.Remove(rolePermission);
        return true;
    }

    public override async Task<Role?> GetByIdAsync(Guid id)
    {
        return await _dbSet
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public override async Task<IEnumerable<Role>> GetAllAsync()
    {
        return await _dbSet
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .OrderBy(r => r.Name)
            .ToListAsync();
    }
}

public class PermissionRepository : BaseRepository<Permission>, IPermissionRepository
{
    public PermissionRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Permission?> GetByNameAsync(string name)
    {
        return await _dbSet.FirstOrDefaultAsync(p => p.Name == name);
    }

    public async Task<IEnumerable<Permission>> GetUnassignedPermissionsAsync(Guid roleId)
    {
        var assignedPermissionIds = await _context.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.PermissionId)
            .ToListAsync();

        return await _dbSet
            .Where(p => !assignedPermissionIds.Contains(p.Id))
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Permission>> GetPermissionsByRoleAsync(Guid roleId)
    {
        return await _context.RolePermissions
            .Include(rp => rp.Permission)
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.Permission)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public override async Task<IEnumerable<Permission>> GetAllAsync()
    {
        return await _dbSet
            .OrderBy(p => p.Name)
            .ToListAsync();
    }
}

public class RefreshTokenRepository : BaseRepository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        return await _dbSet
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == token);
    }

    public async Task<IEnumerable<RefreshToken>> GetActiveTokensByUserAsync(Guid userId)
    {
        return await _dbSet
            .Where(rt => rt.UserId == userId &&
                        rt.RevokedAt == null &&
                        rt.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(rt => rt.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> RevokeAllUserTokensAsync(Guid userId)
    {
        var tokens = await _dbSet
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.RevokedAt = DateTime.UtcNow;
        }

        return tokens.Count;
    }

    public async Task<int> RevokeExpiredTokensAsync()
    {
        var expiredTokens = await _dbSet
            .Where(rt => rt.ExpiresAt <= DateTime.UtcNow && rt.RevokedAt == null)
            .ToListAsync();

        foreach (var token in expiredTokens)
        {
            token.RevokedAt = DateTime.UtcNow;
        }

        return expiredTokens.Count;
    }

    public async Task<bool> IsTokenActiveAsync(string token)
    {
        return await _dbSet
            .AnyAsync(rt => rt.Token == token &&
                           rt.RevokedAt == null &&
                           rt.ExpiresAt > DateTime.UtcNow);
    }

    public override async Task<IEnumerable<RefreshToken>> GetAllAsync()
    {
        return await _dbSet
            .Include(rt => rt.User)
            .OrderByDescending(rt => rt.CreatedAt)
            .ToListAsync();
    }
}