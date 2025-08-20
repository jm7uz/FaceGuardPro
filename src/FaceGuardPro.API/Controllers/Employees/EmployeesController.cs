using FaceGuardPro.Core.Interfaces;
using FaceGuardPro.Shared.Constants;
using FaceGuardPro.Shared.Enums;
using FaceGuardPro.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace FaceGuardPro.API.Controllers;

/// <summary>
/// Employee management endpoints
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class EmployeesController : BaseController
{
    private readonly IEmployeeService _employeeService;
    private readonly ILogger<EmployeesController> _logger;

    public EmployeesController(
        IEmployeeService employeeService,
        ILogger<EmployeesController> logger)
    {
        _employeeService = employeeService;
        _logger = logger;
    }

    /// <summary>
    /// Get all employees with pagination
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    /// <returns>Paginated list of employees</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<IEnumerable<EmployeeDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<ActionResult<PagedResponse<IEnumerable<EmployeeDto>>>> GetAllEmployees(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            RequirePermission(AuthenticationConstants.Permissions.VIEW_EMPLOYEES);

            if (pageSize > DatabaseConstants.MAX_PAGE_SIZE)
                pageSize = DatabaseConstants.MAX_PAGE_SIZE;

            var result = await _employeeService.GetAllEmployeesAsync(pageNumber, pageSize);

            // Direct return for PagedResponse
            if (result.Success)
            {
                return Ok(result);
            }

            // Convert error response
            return result.StatusCode switch
            {
                ApiResponseStatus.BadRequest => BadRequest(result),
                ApiResponseStatus.Unauthorized => Unauthorized(result),
                ApiResponseStatus.Forbidden => StatusCode(403, result),
                ApiResponseStatus.NotFound => NotFound(result),
                ApiResponseStatus.InternalServerError => StatusCode(500, result),
                _ => StatusCode(500, result)
            };
        }
        catch (UnauthorizedAccessException ex)
        {
            var errorResponse = PagedResponse<IEnumerable<EmployeeDto>>.ErrorResult(ex.Message) as PagedResponse<IEnumerable<EmployeeDto>>;
            return Unauthorized(errorResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employees");
            var errorResponse = PagedResponse<IEnumerable<EmployeeDto>>.ErrorResult("Error retrieving employees") as PagedResponse<IEnumerable<EmployeeDto>>;
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Get active employees only
    /// </summary>
    /// <returns>List of active employees</returns>
    [HttpGet("active")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<EmployeeDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<ActionResult<ApiResponse<IEnumerable<EmployeeDto>>>> GetActiveEmployees()
    {
        try
        {
            RequirePermission(AuthenticationConstants.Permissions.VIEW_EMPLOYEES);

            var result = await _employeeService.GetActiveEmployeesAsync();
            return HandleServiceResponse(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized<IEnumerable<EmployeeDto>>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active employees");
            return InternalServerError<IEnumerable<EmployeeDto>>("Error retrieving active employees");
        }
    }

    /// <summary>
    /// Get employee by ID
    /// </summary>
    /// <param name="id">Employee ID</param>
    /// <returns>Employee details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> GetEmployee(Guid id)
    {
        try
        {
            RequirePermission(AuthenticationConstants.Permissions.VIEW_EMPLOYEES);

            var result = await _employeeService.GetEmployeeByIdAsync(id);
            return HandleServiceResponse(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized<EmployeeDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employee {EmployeeId}", id);
            return InternalServerError<EmployeeDto>("Error retrieving employee");
        }
    }

    /// <summary>
    /// Get employee by employee ID
    /// </summary>
    /// <param name="employeeId">Employee ID (string)</param>
    /// <returns>Employee details</returns>
    [HttpGet("by-employee-id/{employeeId}")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> GetEmployeeByEmployeeId(string employeeId)
    {
        try
        {
            RequirePermission(AuthenticationConstants.Permissions.VIEW_EMPLOYEES);

            var result = await _employeeService.GetEmployeeByEmployeeIdAsync(employeeId);
            return HandleServiceResponse(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized<EmployeeDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employee by employee ID {EmployeeId}", employeeId);
            return InternalServerError<EmployeeDto>("Error retrieving employee");
        }
    }

    /// <summary>
    /// Search employees
    /// </summary>
    /// <param name="searchTerm">Search term</param>
    /// <returns>List of matching employees</returns>
    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<EmployeeDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<ActionResult<ApiResponse<IEnumerable<EmployeeDto>>>> SearchEmployees(
        [FromQuery] string searchTerm)
    {
        try
        {
            RequirePermission(AuthenticationConstants.Permissions.VIEW_EMPLOYEES);

            var result = await _employeeService.SearchEmployeesAsync(searchTerm);
            return HandleServiceResponse(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized<IEnumerable<EmployeeDto>>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching employees with term {SearchTerm}", searchTerm);
            return InternalServerError<IEnumerable<EmployeeDto>>("Error searching employees");
        }
    }

    /// <summary>
    /// Create new employee
    /// </summary>
    /// <param name="createEmployeeDto">Employee creation data</param>
    /// <returns>Created employee</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<EmployeeDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> CreateEmployee(
        [FromBody] CreateEmployeeDto createEmployeeDto)
    {
        try
        {
            RequirePermission(AuthenticationConstants.Permissions.MANAGE_EMPLOYEES);

            var validationErrors = GetValidationErrors();
            if (validationErrors != null)
            {
                return BadRequest<EmployeeDto>(validationErrors);
            }

            var result = await _employeeService.CreateEmployeeAsync(createEmployeeDto);

            if (result.Success)
            {
                _logger.LogInformation("Employee created by user {UserId}: {EmployeeId}",
                    CurrentUserId, result.Data?.EmployeeId);
                return Created($"api/v1/employees/{result.Data?.Id}", result);
            }

            return HandleServiceResponse(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized<EmployeeDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating employee");
            return InternalServerError<EmployeeDto>("Error creating employee");
        }
    }

    /// <summary>
    /// Update employee
    /// </summary>
    /// <param name="id">Employee ID</param>
    /// <param name="updateEmployeeDto">Employee update data</param>
    /// <returns>Updated employee</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> UpdateEmployee(
        Guid id,
        [FromBody] UpdateEmployeeDto updateEmployeeDto)
    {
        try
        {
            RequirePermission(AuthenticationConstants.Permissions.MANAGE_EMPLOYEES);

            var validationErrors = GetValidationErrors();
            if (validationErrors != null)
            {
                return BadRequest<EmployeeDto>(validationErrors);
            }

            var result = await _employeeService.UpdateEmployeeAsync(id, updateEmployeeDto);

            if (result.Success)
            {
                _logger.LogInformation("Employee updated by user {UserId}: {EmployeeId}",
                    CurrentUserId, id);
            }

            return HandleServiceResponse(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized<EmployeeDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating employee {EmployeeId}", id);
            return InternalServerError<EmployeeDto>("Error updating employee");
        }
    }

    /// <summary>
    /// Delete employee
    /// </summary>
    /// <param name="id">Employee ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteEmployee(Guid id)
    {
        try
        {
            RequirePermission(AuthenticationConstants.Permissions.MANAGE_EMPLOYEES);

            var result = await _employeeService.DeleteEmployeeAsync(id);

            if (result.Success)
            {
                _logger.LogInformation("Employee deleted by user {UserId}: {EmployeeId}",
                    CurrentUserId, id);
            }

            return HandleServiceResponse(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized<bool>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting employee {EmployeeId}", id);
            return InternalServerError<bool>("Error deleting employee");
        }
    }

    /// <summary>
    /// Deactivate employee
    /// </summary>
    /// <param name="id">Employee ID</param>
    /// <returns>Success status</returns>
    [HttpPatch("{id:guid}/deactivate")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<ActionResult<ApiResponse<bool>>> DeactivateEmployee(Guid id)
    {
        try
        {
            RequirePermission(AuthenticationConstants.Permissions.MANAGE_EMPLOYEES);

            var result = await _employeeService.DeactivateEmployeeAsync(id);

            if (result.Success)
            {
                _logger.LogInformation("Employee deactivated by user {UserId}: {EmployeeId}",
                    CurrentUserId, id);
            }

            return HandleServiceResponse(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized<bool>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating employee {EmployeeId}", id);
            return InternalServerError<bool>("Error deactivating employee");
        }
    }

    /// <summary>
    /// Activate employee
    /// </summary>
    /// <param name="id">Employee ID</param>
    /// <returns>Success status</returns>
    [HttpPatch("{id:guid}/activate")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<ActionResult<ApiResponse<bool>>> ActivateEmployee(Guid id)
    {
        try
        {
            RequirePermission(AuthenticationConstants.Permissions.MANAGE_EMPLOYEES);

            var result = await _employeeService.ActivateEmployeeAsync(id);

            if (result.Success)
            {
                _logger.LogInformation("Employee activated by user {UserId}: {EmployeeId}",
                    CurrentUserId, id);
            }

            return HandleServiceResponse(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized<bool>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating employee {EmployeeId}", id);
            return InternalServerError<bool>("Error activating employee");
        }
    }

    [HttpPost("{id:guid}/photo")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<ActionResult<ApiResponse<string>>> UploadEmployeePhoto(
    Guid id,
    [FromForm] EmployeePhotoUploadRequest request)
    {
        try
        {
            RequirePermission(AuthenticationConstants.Permissions.MANAGE_EMPLOYEES);

            if (request.Photo == null || request.Photo.Length == 0)
                return BadRequest<string>("Photo file is required");

            if (request.Photo.Length > ImageProcessingConstants.MAX_FILE_SIZE_MB * 1024 * 1024)
                return BadRequest<string>($"File size cannot exceed {ImageProcessingConstants.MAX_FILE_SIZE_MB}MB");

            using var memoryStream = new MemoryStream();
            await request.Photo.CopyToAsync(memoryStream);
            var photoData = memoryStream.ToArray();

            var result = await _employeeService.UploadEmployeePhotoAsync(id, photoData, request.Photo.FileName);

            if (result.Success)
            {
                _logger.LogInformation("Photo uploaded for employee {EmployeeId} by user {UserId}",
                    id, CurrentUserId);
            }

            return HandleServiceResponse(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized<string>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading photo for employee {EmployeeId}", id);
            return InternalServerError<string>("Error uploading photo");
        }
    }

    /// <summary>
    /// Get employee statistics
    /// </summary>
    /// <returns>Employee statistics</returns>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeStatsDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<ActionResult<ApiResponse<EmployeeStatsDto>>> GetEmployeeStatistics()
    {
        try
        {
            RequirePermission(AuthenticationConstants.Permissions.VIEW_EMPLOYEES);

            var result = await _employeeService.GetEmployeeStatisticsAsync();
            return HandleServiceResponse(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized<EmployeeStatsDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employee statistics");
            return InternalServerError<EmployeeStatsDto>("Error retrieving statistics");
        }
    }

    /// <summary>
    /// Check if employee ID is unique
    /// </summary>
    /// <param name="employeeId">Employee ID to check</param>
    /// <param name="excludeId">Employee ID to exclude from check</param>
    /// <returns>True if unique</returns>
    [HttpGet("check-employee-id/{employeeId}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<ActionResult<ApiResponse<bool>>> CheckEmployeeIdUnique(
        string employeeId,
        [FromQuery] Guid? excludeId = null)
    {
        try
        {
            RequirePermission(AuthenticationConstants.Permissions.VIEW_EMPLOYEES);

            var result = await _employeeService.IsEmployeeIdUniqueAsync(employeeId, excludeId);
            return HandleServiceResponse(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized<bool>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking employee ID uniqueness");
            return InternalServerError<bool>("Error checking employee ID");
        }
    }
}


public class EmployeePhotoUploadRequest
{
    [Required]
    public IFormFile Photo { get; set; } = default!;
}