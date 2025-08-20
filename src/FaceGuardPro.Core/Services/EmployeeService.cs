using AutoMapper;
using Microsoft.Extensions.Logging;
using FaceGuardPro.Core.Interfaces;
using FaceGuardPro.Data.UnitOfWork;
using FaceGuardPro.Data.Entities;
using FaceGuardPro.Shared.Models;
using FaceGuardPro.Shared.Enums;
using FaceGuardPro.Shared.Constants;

namespace FaceGuardPro.Core.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<EmployeeService> _logger;
    private readonly IFileStorageService _fileStorageService;

    public EmployeeService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<EmployeeService> logger,
        IFileStorageService fileStorageService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _fileStorageService = fileStorageService;
    }

    public async Task<ApiResponse<EmployeeDto>> GetEmployeeByIdAsync(Guid id)
    {
        try
        {
            var employee = await _unitOfWork.Employees.GetByIdAsync(id);
            if (employee == null)
            {
                return ApiResponse<EmployeeDto>.NotFoundResult("Employee not found");
            }

            var employeeDto = _mapper.Map<EmployeeDto>(employee);
            return ApiResponse<EmployeeDto>.SuccessResult(employeeDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employee with ID: {EmployeeId}", id);
            return ApiResponse<EmployeeDto>.ErrorResult("An error occurred while retrieving the employee");
        }
    }

    public async Task<ApiResponse<EmployeeDto>> GetEmployeeByEmployeeIdAsync(string employeeId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(employeeId))
            {
                return ApiResponse<EmployeeDto>.BadRequestResult("Employee ID cannot be empty");
            }

            var employee = await _unitOfWork.Employees.GetByEmployeeIdAsync(employeeId);
            if (employee == null)
            {
                return ApiResponse<EmployeeDto>.NotFoundResult("Employee not found");
            }

            var employeeDto = _mapper.Map<EmployeeDto>(employee);
            return ApiResponse<EmployeeDto>.SuccessResult(employeeDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employee with Employee ID: {EmployeeId}", employeeId);
            return ApiResponse<EmployeeDto>.ErrorResult("An error occurred while retrieving the employee");
        }
    }

    public async Task<ApiResponse<EmployeeDto>> GetEmployeeByEmailAsync(string email)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return ApiResponse<EmployeeDto>.BadRequestResult("Email cannot be empty");
            }

            var employee = await _unitOfWork.Employees.GetByEmailAsync(email);
            if (employee == null)
            {
                return ApiResponse<EmployeeDto>.NotFoundResult("Employee not found");
            }

            var employeeDto = _mapper.Map<EmployeeDto>(employee);
            return ApiResponse<EmployeeDto>.SuccessResult(employeeDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employee with email: {Email}", email);
            return ApiResponse<EmployeeDto>.ErrorResult("An error occurred while retrieving the employee");
        }
    }

    public async Task<PagedResponse<IEnumerable<EmployeeDto>>> GetAllEmployeesAsync(int pageNumber = 1, int pageSize = 20)
    {
        try
        {
            if (pageSize > DatabaseConstants.MAX_PAGE_SIZE)
                pageSize = DatabaseConstants.MAX_PAGE_SIZE;

            var totalCount = await _unitOfWork.Employees.CountAsync();
            var employees = await _unitOfWork.Employees.GetPagedAsync(pageNumber, pageSize);
            var employeeDtos = _mapper.Map<IEnumerable<EmployeeDto>>(employees);

            return PagedResponse<IEnumerable<EmployeeDto>>.SuccesResponse(
                employeeDtos, totalCount, pageNumber, pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employees page {PageNumber}", pageNumber);
            return PagedResponse<IEnumerable<EmployeeDto>>.ErrorResult("An error occurred while retrieving employees") as PagedResponse<IEnumerable<EmployeeDto>>;
        }
    }

    public async Task<ApiResponse<IEnumerable<EmployeeDto>>> GetActiveEmployeesAsync()
    {
        try
        {
            var employees = await _unitOfWork.Employees.GetActiveEmployeesAsync();
            var employeeDtos = _mapper.Map<IEnumerable<EmployeeDto>>(employees);
            return ApiResponse<IEnumerable<EmployeeDto>>.SuccessResult(employeeDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active employees");
            return ApiResponse<IEnumerable<EmployeeDto>>.ErrorResult("An error occurred while retrieving active employees");
        }
    }

    public async Task<ApiResponse<IEnumerable<EmployeeDto>>> GetEmployeesByDepartmentAsync(string department)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(department))
            {
                return ApiResponse<IEnumerable<EmployeeDto>>.BadRequestResult("Department cannot be empty");
            }

            var employees = await _unitOfWork.Employees.GetByDepartmentAsync(department);
            var employeeDtos = _mapper.Map<IEnumerable<EmployeeDto>>(employees);
            return ApiResponse<IEnumerable<EmployeeDto>>.SuccessResult(employeeDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employees for department: {Department}", department);
            return ApiResponse<IEnumerable<EmployeeDto>>.ErrorResult("An error occurred while retrieving employees");
        }
    }

    public async Task<ApiResponse<IEnumerable<EmployeeDto>>> SearchEmployeesAsync(string searchTerm)
    {
        try
        {
            var employees = await _unitOfWork.Employees.SearchEmployeesAsync(searchTerm);
            var employeeDtos = _mapper.Map<IEnumerable<EmployeeDto>>(employees);
            return ApiResponse<IEnumerable<EmployeeDto>>.SuccessResult(employeeDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching employees with term: {SearchTerm}", searchTerm);
            return ApiResponse<IEnumerable<EmployeeDto>>.ErrorResult("An error occurred while searching employees");
        }
    }

    public async Task<ApiResponse<EmployeeDto>> CreateEmployeeAsync(CreateEmployeeDto createEmployeeDto)
    {
        try
        {
            // Validate unique constraints
            var validationResult = await ValidateEmployeeDataAsync(createEmployeeDto);
            if (!validationResult.Success)
            {
                return ApiResponse<EmployeeDto>.ErrorResult(validationResult.Errors, "Validation failed");
            }

            var employee = _mapper.Map<Employee>(createEmployeeDto);
            employee.Status = EmployeeStatus.Active;

            await _unitOfWork.Employees.AddAsync(employee);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Employee created successfully: {EmployeeId}", employee.EmployeeId);

            var employeeDto = _mapper.Map<EmployeeDto>(employee);
            return ApiResponse<EmployeeDto>.SuccessResult(employeeDto, "Employee created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating employee: {EmployeeId}", createEmployeeDto.EmployeeId);
            return ApiResponse<EmployeeDto>.ErrorResult("An error occurred while creating the employee");
        }
    }

    public async Task<ApiResponse<EmployeeDto>> UpdateEmployeeAsync(Guid id, UpdateEmployeeDto updateEmployeeDto)
    {
        try
        {
            var employee = await _unitOfWork.Employees.GetByIdAsync(id);
            if (employee == null)
            {
                return ApiResponse<EmployeeDto>.NotFoundResult("Employee not found");
            }

            // Validate unique constraints
            var validationResult = await ValidateEmployeeUpdateDataAsync(id, updateEmployeeDto);
            if (!validationResult.Success)
            {
                return ApiResponse<EmployeeDto>.ErrorResult(validationResult.Errors, "Validation failed");
            }

            _mapper.Map(updateEmployeeDto, employee);
            employee.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Employees.UpdateAsync(employee);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Employee updated successfully: {EmployeeId}", employee.EmployeeId);

            var employeeDto = _mapper.Map<EmployeeDto>(employee);
            return ApiResponse<EmployeeDto>.SuccessResult(employeeDto, "Employee updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating employee with ID: {EmployeeId}", id);
            return ApiResponse<EmployeeDto>.ErrorResult("An error occurred while updating the employee");
        }
    }

    public async Task<ApiResponse<bool>> DeleteEmployeeAsync(Guid id)
    {
        try
        {
            var employee = await _unitOfWork.Employees.GetByIdAsync(id);
            if (employee == null)
            {
                return ApiResponse<bool>.NotFoundResult("Employee not found");
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // Delete face templates first
                await _unitOfWork.FaceTemplates.DeleteByEmployeeIdAsync(id);

                // Delete employee photo if exists
                if (!string.IsNullOrEmpty(employee.PhotoPath))
                {
                    await _fileStorageService.DeleteFileAsync(employee.PhotoPath);
                }

                // Delete employee
                await _unitOfWork.Employees.DeleteAsync(employee);
                await _unitOfWork.SaveChangesAsync();

                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("Employee deleted successfully: {EmployeeId}", employee.EmployeeId);
                return ApiResponse<bool>.SuccessResult(true, "Employee deleted successfully");
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting employee with ID: {EmployeeId}", id);
            return ApiResponse<bool>.ErrorResult("An error occurred while deleting the employee");
        }
    }

    public async Task<ApiResponse<bool>> DeactivateEmployeeAsync(Guid id)
    {
        try
        {
            var employee = await _unitOfWork.Employees.GetByIdAsync(id);
            if (employee == null)
            {
                return ApiResponse<bool>.NotFoundResult("Employee not found");
            }

            employee.Status = EmployeeStatus.Inactive;
            employee.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Employees.UpdateAsync(employee);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Employee deactivated: {EmployeeId}", employee.EmployeeId);
            return ApiResponse<bool>.SuccessResult(true, "Employee deactivated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating employee with ID: {EmployeeId}", id);
            return ApiResponse<bool>.ErrorResult("An error occurred while deactivating the employee");
        }
    }

    public async Task<ApiResponse<bool>> ActivateEmployeeAsync(Guid id)
    {
        try
        {
            var employee = await _unitOfWork.Employees.GetByIdAsync(id);
            if (employee == null)
            {
                return ApiResponse<bool>.NotFoundResult("Employee not found");
            }

            employee.Status = EmployeeStatus.Active;
            employee.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Employees.UpdateAsync(employee);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Employee activated: {EmployeeId}", employee.EmployeeId);
            return ApiResponse<bool>.SuccessResult(true, "Employee activated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating employee with ID: {EmployeeId}", id);
            return ApiResponse<bool>.ErrorResult("An error occurred while activating the employee");
        }
    }

    public async Task<ApiResponse<string>> UploadEmployeePhotoAsync(Guid employeeId, byte[] photoData, string fileName)
    {
        try
        {
            var employee = await _unitOfWork.Employees.GetByIdAsync(employeeId);
            if (employee == null)
            {
                return ApiResponse<string>.NotFoundResult("Employee not found");
            }

            // Validate image
            var imageValidation = await _fileStorageService.ValidateImageAsync(photoData);
            if (!imageValidation.IsValid)
            {
                return ApiResponse<string>.BadRequestResult(imageValidation.ErrorMessage);
            }

            // Delete old photo if exists
            if (!string.IsNullOrEmpty(employee.PhotoPath))
            {
                await _fileStorageService.DeleteFileAsync(employee.PhotoPath);
            }

            // Save new photo
            var photoPath = await _fileStorageService.SaveEmployeePhotoAsync(employeeId, photoData, fileName);

            employee.PhotoPath = photoPath;
            employee.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Employees.UpdateAsync(employee);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Photo uploaded for employee: {EmployeeId}", employee.EmployeeId);
            return ApiResponse<string>.SuccessResult(photoPath, "Photo uploaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading photo for employee: {EmployeeId}", employeeId);
            return ApiResponse<string>.ErrorResult("An error occurred while uploading the photo");
        }
    }

    public async Task<ApiResponse<byte[]>> GetEmployeePhotoAsync(Guid employeeId)
    {
        try
        {
            var employee = await _unitOfWork.Employees.GetByIdAsync(employeeId);
            if (employee == null)
            {
                return ApiResponse<byte[]>.NotFoundResult("Employee not found");
            }

            if (string.IsNullOrEmpty(employee.PhotoPath))
            {
                return ApiResponse<byte[]>.NotFoundResult("Employee photo not found");
            }

            var photoData = await _fileStorageService.GetFileAsync(employee.PhotoPath);
            if (photoData == null)
            {
                return ApiResponse<byte[]>.NotFoundResult("Photo file not found");
            }

            return ApiResponse<byte[]>.SuccessResult(photoData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving photo for employee: {EmployeeId}", employeeId);
            return ApiResponse<byte[]>.ErrorResult("An error occurred while retrieving the photo");
        }
    }

    public async Task<ApiResponse<bool>> DeleteEmployeePhotoAsync(Guid employeeId)
    {
        try
        {
            var employee = await _unitOfWork.Employees.GetByIdAsync(employeeId);
            if (employee == null)
            {
                return ApiResponse<bool>.NotFoundResult("Employee not found");
            }

            if (string.IsNullOrEmpty(employee.PhotoPath))
            {
                return ApiResponse<bool>.SuccessResult(true, "No photo to delete");
            }

            await _fileStorageService.DeleteFileAsync(employee.PhotoPath);

            employee.PhotoPath = null;
            employee.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Employees.UpdateAsync(employee);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Photo deleted for employee: {EmployeeId}", employee.EmployeeId);
            return ApiResponse<bool>.SuccessResult(true, "Photo deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting photo for employee: {EmployeeId}", employeeId);
            return ApiResponse<bool>.ErrorResult("An error occurred while deleting the photo");
        }
    }

    public async Task<ApiResponse<bool>> HasFaceTemplateAsync(Guid employeeId)
    {
        try
        {
            var hasTemplate = await _unitOfWork.FaceTemplates.HasFaceTemplateAsync(employeeId);
            return ApiResponse<bool>.SuccessResult(hasTemplate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking face template for employee: {EmployeeId}", employeeId);
            return ApiResponse<bool>.ErrorResult("An error occurred while checking face template");
        }
    }

    public async Task<ApiResponse<IEnumerable<EmployeeDto>>> GetEmployeesWithFaceTemplatesAsync()
    {
        try
        {
            var employees = await _unitOfWork.Employees.GetEmployeesWithFaceTemplatesAsync();
            var employeeDtos = _mapper.Map<IEnumerable<EmployeeDto>>(employees);
            return ApiResponse<IEnumerable<EmployeeDto>>.SuccessResult(employeeDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employees with face templates");
            return ApiResponse<IEnumerable<EmployeeDto>>.ErrorResult("An error occurred while retrieving employees");
        }
    }

    public async Task<ApiResponse<IEnumerable<EmployeeDto>>> GetEmployeesWithoutFaceTemplatesAsync()
    {
        try
        {
            var allActiveEmployees = await _unitOfWork.Employees.GetActiveEmployeesAsync();
            var employeesWithoutTemplates = allActiveEmployees.Where(e => !e.HasFaceTemplate);
            var employeeDtos = _mapper.Map<IEnumerable<EmployeeDto>>(employeesWithoutTemplates);
            return ApiResponse<IEnumerable<EmployeeDto>>.SuccessResult(employeeDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employees without face templates");
            return ApiResponse<IEnumerable<EmployeeDto>>.ErrorResult("An error occurred while retrieving employees");
        }
    }

    public async Task<ApiResponse<bool>> IsEmployeeIdUniqueAsync(string employeeId, Guid? excludeId = null)
    {
        try
        {
            var isUnique = await _unitOfWork.Employees.IsEmployeeIdUniqueAsync(employeeId, excludeId);
            return ApiResponse<bool>.SuccessResult(isUnique);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking employee ID uniqueness: {EmployeeId}", employeeId);
            return ApiResponse<bool>.ErrorResult("An error occurred while checking employee ID");
        }
    }

    public async Task<ApiResponse<bool>> IsEmailUniqueAsync(string email, Guid? excludeId = null)
    {
        try
        {
            var isUnique = await _unitOfWork.Employees.IsEmailUniqueAsync(email, excludeId);
            return ApiResponse<bool>.SuccessResult(isUnique);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking email uniqueness: {Email}", email);
            return ApiResponse<bool>.ErrorResult("An error occurred while checking email");
        }
    }

    public async Task<ApiResponse<bool>> ValidateEmployeeDataAsync(CreateEmployeeDto employeeDto)
    {
        var errors = new List<string>();

        // Check employee ID uniqueness
        var employeeIdUnique = await _unitOfWork.Employees.IsEmployeeIdUniqueAsync(employeeDto.EmployeeId);
        if (!employeeIdUnique)
        {
            errors.Add("Employee ID already exists");
        }

        // Check email uniqueness
        var emailUnique = await _unitOfWork.Employees.IsEmailUniqueAsync(employeeDto.Email);
        if (!emailUnique)
        {
            errors.Add("Email already exists");
        }

        if (errors.Any())
        {
            return ApiResponse<bool>.ErrorResult(errors, "Validation failed");
        }

        return ApiResponse<bool>.SuccessResult(true);
    }

    public async Task<ApiResponse<bool>> ValidateEmployeeUpdateDataAsync(Guid id, UpdateEmployeeDto employeeDto)
    {
        var errors = new List<string>();

        // Check email uniqueness (excluding current employee)
        var emailUnique = await _unitOfWork.Employees.IsEmailUniqueAsync(employeeDto.Email, id);
        if (!emailUnique)
        {
            errors.Add("Email already exists");
        }

        if (errors.Any())
        {
            return ApiResponse<bool>.ErrorResult(errors, "Validation failed");
        }

        return ApiResponse<bool>.SuccessResult(true);
    }

    public async Task<ApiResponse<EmployeeStatsDto>> GetEmployeeStatisticsAsync()
    {
        try
        {
            var totalEmployees = await _unitOfWork.Employees.CountAsync();
            var activeEmployees = await _unitOfWork.Employees.CountAsync(e => e.Status == EmployeeStatus.Active);
            var inactiveEmployees = totalEmployees - activeEmployees;
            var employeesWithTemplates = await _unitOfWork.Employees.CountAsync(e => e.FaceTemplates.Any());

            var stats = new EmployeeStatsDto
            {
                TotalEmployees = totalEmployees,
                ActiveEmployees = activeEmployees,
                InactiveEmployees = inactiveEmployees,
                EmployeesWithFaceTemplates = employeesWithTemplates,
                EmployeesWithoutFaceTemplates = totalEmployees - employeesWithTemplates,
                FaceTemplatePercentage = totalEmployees > 0 ? (double)employeesWithTemplates / totalEmployees * 100 : 0
            };

            // Get employees by status
            foreach (EmployeeStatus status in Enum.GetValues<EmployeeStatus>())
            {
                var count = await _unitOfWork.Employees.CountAsync(e => e.Status == status);
                stats.EmployeesByStatus[status.ToString()] = count;
            }

            return ApiResponse<EmployeeStatsDto>.SuccessResult(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating employee statistics");
            return ApiResponse<EmployeeStatsDto>.ErrorResult("An error occurred while generating statistics");
        }
    }

    public async Task<ApiResponse<IEnumerable<DepartmentStatsDto>>> GetDepartmentStatisticsAsync()
    {
        try
        {
            var allEmployees = await _unitOfWork.Employees.GetAllAsync();
            var departmentStats = allEmployees
                .GroupBy(e => e.Department)
                .Select(g => new DepartmentStatsDto
                {
                    Department = g.Key,
                    TotalEmployees = g.Count(),
                    ActiveEmployees = g.Count(e => e.Status == EmployeeStatus.Active),
                    EmployeesWithFaceTemplates = g.Count(e => e.HasFaceTemplate),
                    FaceTemplatePercentage = g.Count() > 0 ? (double)g.Count(e => e.HasFaceTemplate) / g.Count() * 100 : 0,
                    RecentlyJoined = _mapper.Map<List<EmployeeDto>>(
                        g.Where(e => e.JoinDate >= DateTime.Now.AddMonths(-3))
                         .OrderByDescending(e => e.JoinDate)
                         .Take(5))
                })
                .OrderBy(d => d.Department)
                .ToList();

            return ApiResponse<IEnumerable<DepartmentStatsDto>>.SuccessResult(departmentStats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating department statistics");
            return ApiResponse<IEnumerable<DepartmentStatsDto>>.ErrorResult("An error occurred while generating statistics");
        }
    }
}