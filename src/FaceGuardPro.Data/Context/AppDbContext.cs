using FaceGuardPro.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace FaceGuardPro.Data.Context;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<Employee> Employees { get; set; }
    public DbSet<FaceTemplate> FaceTemplates { get; set; }
    public DbSet<AuthenticationLog> AuthenticationLogs { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // PostgreSQL uchun naming convention
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            // Table names to snake_case
            entity.SetTableName(entity.GetTableName()?.ToSnakeCase());

            // Column names to snake_case
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(property.GetColumnName().ToSnakeCase());
            }
        }

        // Employee configuration
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasIndex(e => e.EmployeeId).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Status).HasConversion<int>();

            // PostgreSQL specific configurations
            entity.Property(e => e.Id).HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.JoinDate).HasColumnType("timestamp with time zone");
        });

        // FaceTemplate configuration
        modelBuilder.Entity<FaceTemplate>(entity =>
        {
            entity.HasOne(f => f.Employee)
                  .WithMany(e => e.FaceTemplates)
                  .HasForeignKey(f => f.EmployeeId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.Id).HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnType("timestamp with time zone");
            entity.Property(e => e.TemplateData).HasColumnType("bytea");
        });

        // AuthenticationLog configuration
        modelBuilder.Entity<AuthenticationLog>(entity =>
        {
            entity.HasOne(a => a.Employee)
                  .WithMany(e => e.AuthenticationLogs)
                  .HasForeignKey(a => a.EmployeeId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.Id).HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.AttemptedAt).HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();

            entity.Property(e => e.Id).HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.LastLoginAt).HasColumnType("timestamp with time zone");
            entity.Property(e => e.UpdatedAt).HasColumnType("timestamp with time zone");
        });

        // UserRole configuration
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasIndex(ur => new { ur.UserId, ur.RoleId }).IsUnique();

            entity.HasOne(ur => ur.User)
                  .WithMany(u => u.UserRoles)
                  .HasForeignKey(ur => ur.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ur => ur.Role)
                  .WithMany(r => r.UserRoles)
                  .HasForeignKey(ur => ur.RoleId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.Id).HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.AssignedAt).HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // RolePermission configuration
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasIndex(rp => new { rp.RoleId, rp.PermissionId }).IsUnique();

            entity.HasOne(rp => rp.Role)
                  .WithMany(r => r.RolePermissions)
                  .HasForeignKey(rp => rp.RoleId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(rp => rp.Permission)
                  .WithMany(p => p.RolePermissions)
                  .HasForeignKey(rp => rp.PermissionId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.Id).HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.AssignedAt).HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Role configuration
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(r => r.Name).IsUnique();
            entity.Property(e => e.Id).HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Permission configuration
        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasIndex(p => p.Name).IsUnique();
            entity.Property(e => e.Id).HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // RefreshToken configuration
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(rt => rt.Token).IsUnique();

            entity.HasOne(rt => rt.User)
                  .WithMany(u => u.RefreshTokens)
                  .HasForeignKey(rt => rt.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.Id).HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.ExpiresAt).HasColumnType("timestamp with time zone");
            entity.Property(e => e.RevokedAt).HasColumnType("timestamp with time zone");
        });

        // Seed initial data
        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        // Seed Roles
        var adminRoleId = Guid.Parse("550e8400-e29b-41d4-a716-446655440001");
        var operatorRoleId = Guid.Parse("550e8400-e29b-41d4-a716-446655440002");
        var viewerRoleId = Guid.Parse("550e8400-e29b-41d4-a716-446655440003");

        modelBuilder.Entity<Role>().HasData(
            new Role { Id = adminRoleId, Name = "Admin", Description = "Full system access", CreatedAt = DateTime.UtcNow },
            new Role { Id = operatorRoleId, Name = "Operator", Description = "Employee management and authentication", CreatedAt = DateTime.UtcNow },
            new Role { Id = viewerRoleId, Name = "Viewer", Description = "Read-only access", CreatedAt = DateTime.UtcNow }
        );

        // Seed Permissions
        var permissions = new[]
        {
            new Permission { Id = Guid.Parse("660e8400-e29b-41d4-a716-446655440001"), Name = "ManageEmployees", Description = "Create, update, delete employees", CreatedAt = DateTime.UtcNow },
            new Permission { Id = Guid.Parse("660e8400-e29b-41d4-a716-446655440002"), Name = "ViewEmployees", Description = "View employee information", CreatedAt = DateTime.UtcNow },
            new Permission { Id = Guid.Parse("660e8400-e29b-41d4-a716-446655440003"), Name = "ManageFaceTemplates", Description = "Manage face templates", CreatedAt = DateTime.UtcNow },
            new Permission { Id = Guid.Parse("660e8400-e29b-41d4-a716-446655440004"), Name = "PerformAuthentication", Description = "Perform face authentication", CreatedAt = DateTime.UtcNow },
            new Permission { Id = Guid.Parse("660e8400-e29b-41d4-a716-446655440005"), Name = "ViewReports", Description = "View system reports", CreatedAt = DateTime.UtcNow },
            new Permission { Id = Guid.Parse("660e8400-e29b-41d4-a716-446655440006"), Name = "ManageSystem", Description = "System administration", CreatedAt = DateTime.UtcNow }
        };

        modelBuilder.Entity<Permission>().HasData(permissions);

        // Assign all permissions to Admin role
        var adminRolePermissions = permissions.Select((p, index) => new RolePermission
        {
            Id = Guid.Parse($"770e8400-e29b-41d4-a716-44665544000{index + 1}"),
            RoleId = adminRoleId,
            PermissionId = p.Id,
            AssignedAt = DateTime.UtcNow
        }).ToArray();

        // Assign limited permissions to Operator role
        var operatorPermissions = permissions.Where(p => p.Name != "ManageSystem").Select((p, index) => new RolePermission
        {
            Id = Guid.Parse($"780e8400-e29b-41d4-a716-44665544000{index + 1}"),
            RoleId = operatorRoleId,
            PermissionId = p.Id,
            AssignedAt = DateTime.UtcNow
        }).ToArray();

        // Assign view permissions to Viewer role
        var viewerPermissions = permissions.Where(p => p.Name.StartsWith("View")).Select((p, index) => new RolePermission
        {
            Id = Guid.Parse($"790e8400-e29b-41d4-a716-44665544000{index + 1}"),
            RoleId = viewerRoleId,
            PermissionId = p.Id,
            AssignedAt = DateTime.UtcNow
        }).ToArray();

        modelBuilder.Entity<RolePermission>().HasData(adminRolePermissions.Concat(operatorPermissions).Concat(viewerPermissions));

        // Seed default admin user
        var adminUserId = Guid.Parse("440e8400-e29b-41d4-a716-446655440001");
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = adminUserId,
                Username = "admin",
                Email = "admin@faceguardpro.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"), // Default password
                FirstName = "System",
                LastName = "Administrator",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        );

        // Assign admin role to default user
        modelBuilder.Entity<UserRole>().HasData(
            new UserRole
            {
                Id = Guid.Parse("880e8400-e29b-41d4-a716-446655440001"),
                UserId = adminUserId,
                RoleId = adminRoleId,
                AssignedAt = DateTime.UtcNow
            }
        );
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is Employee employee)
            {
                if (entry.State == EntityState.Added)
                {
                    employee.CreatedAt = DateTime.UtcNow;
                }
                employee.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is FaceTemplate faceTemplate)
            {
                if (entry.State == EntityState.Modified)
                {
                    faceTemplate.UpdatedAt = DateTime.UtcNow;
                }
            }
            else if (entry.Entity is User user)
            {
                if (entry.State == EntityState.Modified)
                {
                    user.UpdatedAt = DateTime.UtcNow;
                }
            }
        }
    }
}

// Extension method for snake_case conversion
public static class StringExtensions
{
    public static string ToSnakeCase(this string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var result = new StringBuilder();
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            if (char.IsUpper(c) && i > 0)
            {
                result.Append('_');
            }
            result.Append(char.ToLower(c));
        }
        return result.ToString();
    }
}