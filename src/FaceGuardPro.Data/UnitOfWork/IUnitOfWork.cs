using FaceGuardPro.Data.Repositories;

namespace FaceGuardPro.Data.UnitOfWork;

public interface IUnitOfWork : IDisposable
{
    // Repository properties
    IEmployeeRepository Employees { get; }
    IFaceTemplateRepository FaceTemplates { get; }
    IUserRepository Users { get; }
    IRoleRepository Roles { get; }
    IPermissionRepository Permissions { get; }
    IAuthenticationLogRepository AuthenticationLogs { get; }
    IRefreshTokenRepository RefreshTokens { get; }

    // Transaction management
    Task<int> SaveChangesAsync();
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    // Transaction support
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();

    // Generic repository access
    IRepository<T> Repository<T>() where T : class;
}

public class UnitOfWork : IUnitOfWork
{
    private readonly Data.Context.AppDbContext _context;
    private Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? _transaction;
    private bool _disposed = false;

    // Repository instances
    private IEmployeeRepository? _employees;
    private IFaceTemplateRepository? _faceTemplates;
    private IUserRepository? _users;
    private IRoleRepository? _roles;
    private IPermissionRepository? _permissions;
    private IAuthenticationLogRepository? _authenticationLogs;
    private IRefreshTokenRepository? _refreshTokens;

    public UnitOfWork(Data.Context.AppDbContext context)
    {
        _context = context;
    }

    // Repository properties with lazy initialization
    public IEmployeeRepository Employees =>
        _employees ??= new EmployeeRepository(_context);

    public IFaceTemplateRepository FaceTemplates =>
        _faceTemplates ??= new FaceTemplateRepository(_context);

    public IUserRepository Users =>
        _users ??= new UserRepository(_context);

    public IRoleRepository Roles =>
        _roles ??= new RoleRepository(_context);

    public IPermissionRepository Permissions =>
        _permissions ??= new PermissionRepository(_context);

    public IAuthenticationLogRepository AuthenticationLogs =>
        _authenticationLogs ??= new AuthenticationLogRepository(_context);

    public IRefreshTokenRepository RefreshTokens =>
        _refreshTokens ??= new RefreshTokenRepository(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public IRepository<T> Repository<T>() where T : class
    {
        return new BaseRepository<T>(_context);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}