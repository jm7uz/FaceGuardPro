using FaceGuardPro.Shared.Models;

namespace FaceGuardPro.Core.Interfaces;

public interface IEmployeeService
{
    // CRUD Operations
    Task<ApiResponse<EmployeeDto>> GetEmployeeByIdAsync(Guid id);
    Task<ApiResponse<EmployeeDto>> GetEmployeeByEmployeeIdAsync(string employeeId);
    Task<ApiResponse<EmployeeDto>> GetEmployeeByEmailAsync(string email);
    Task<PagedResponse<IEnumerable<EmployeeDto>>> GetAllEmployeesAsync(int pageNumber = 1, int pageSize = 20);
    Task<ApiResponse<IEnumerable<EmployeeDto>>> GetActiveEmployeesAsync();
    Task<ApiResponse<IEnumerable<EmployeeDto>>> GetEmployeesByDepartmentAsync(string department);
    Task<ApiResponse<IEnumerable<EmployeeDto>>> SearchEmployeesAsync(string searchTerm);

    // Employee Management
    Task<ApiResponse<EmployeeDto>> CreateEmployeeAsync(CreateEmployeeDto createEmployeeDto);
    Task<ApiResponse<EmployeeDto>> UpdateEmployeeAsync(Guid id, UpdateEmployeeDto updateEmployeeDto);
    Task<ApiResponse<bool>> DeleteEmployeeAsync(Guid id);
    Task<ApiResponse<bool>> DeactivateEmployeeAsync(Guid id);
    Task<ApiResponse<bool>> ActivateEmployeeAsync(Guid id);

    // Photo Management
    Task<ApiResponse<string>> UploadEmployeePhotoAsync(Guid employeeId, byte[] photoData, string fileName);
    Task<ApiResponse<byte[]>> GetEmployeePhotoAsync(Guid employeeId);
    Task<ApiResponse<bool>> DeleteEmployeePhotoAsync(Guid employeeId);

    // Face Template Management
    Task<ApiResponse<bool>> HasFaceTemplateAsync(Guid employeeId);
    Task<ApiResponse<IEnumerable<EmployeeDto>>> GetEmployeesWithFaceTemplatesAsync();
    Task<ApiResponse<IEnumerable<EmployeeDto>>> GetEmployeesWithoutFaceTemplatesAsync();

    // Validation
    Task<ApiResponse<bool>> IsEmployeeIdUniqueAsync(string employeeId, Guid? excludeId = null);
    Task<ApiResponse<bool>> IsEmailUniqueAsync(string email, Guid? excludeId = null);
    Task<ApiResponse<bool>> ValidateEmployeeDataAsync(CreateEmployeeDto employeeDto);
    Task<ApiResponse<bool>> ValidateEmployeeUpdateDataAsync(Guid id, UpdateEmployeeDto employeeDto);

    // Statistics
    Task<ApiResponse<EmployeeStatsDto>> GetEmployeeStatisticsAsync();
    Task<ApiResponse<IEnumerable<DepartmentStatsDto>>> GetDepartmentStatisticsAsync();
}

public class EmployeeStatsDto
{
    public int TotalEmployees { get; set; }
    public int ActiveEmployees { get; set; }
    public int InactiveEmployees { get; set; }
    public int EmployeesWithFaceTemplates { get; set; }
    public int EmployeesWithoutFaceTemplates { get; set; }
    public double FaceTemplatePercentage { get; set; }
    public Dictionary<string, int> EmployeesByStatus { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class DepartmentStatsDto
{
    public string Department { get; set; } = string.Empty;
    public int TotalEmployees { get; set; }
    public int ActiveEmployees { get; set; }
    public int EmployeesWithFaceTemplates { get; set; }
    public double FaceTemplatePercentage { get; set; }
    public List<EmployeeDto> RecentlyJoined { get; set; } = new();
}