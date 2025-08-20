using Microsoft.AspNetCore.Mvc;
using FaceGuardPro.Shared.Models;
using FaceGuardPro.Shared.Enums;
using System.Security.Claims;

namespace FaceGuardPro.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public abstract class BaseController : ControllerBase
{
    /// <summary>
    /// Get current user ID from JWT claims
    /// </summary>
    protected Guid? CurrentUserId
    {
        get
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }

    /// <summary>
    /// Get current username from JWT claims
    /// </summary>
    protected string? CurrentUsername => User.FindFirst(ClaimTypes.Name)?.Value;

    /// <summary>
    /// Get current user email from JWT claims
    /// </summary>
    protected string? CurrentUserEmail => User.FindFirst(ClaimTypes.Email)?.Value;

    /// <summary>
    /// Get current user roles from JWT claims
    /// </summary>
    protected IEnumerable<string> CurrentUserRoles =>
        User.FindAll(ClaimTypes.Role).Select(c => c.Value);

    /// <summary>
    /// Check if current user has specific role
    /// </summary>
    protected bool HasRole(string role) => CurrentUserRoles.Contains(role);

    /// <summary>
    /// Check if current user has specific permission
    /// </summary>
    protected bool HasPermission(string permission) =>
        User.FindAll("permission").Any(c => c.Value == permission);

    /// <summary>
    /// Return success response with data
    /// </summary>
    protected ActionResult<ApiResponse<T>> Success<T>(T data, string message = "Operation completed successfully")
    {
        return Ok(ApiResponse<T>.SuccessResult(data, message));
    }

    /// <summary>
    /// Return success response without data
    /// </summary>
    protected ActionResult<ApiResponse<object>> Success(string message = "Operation completed successfully")
    {
        return Ok(ApiResponse<object>.SuccessResult(message));
    }

    /// <summary>
    /// Return paged success response
    /// </summary>
    protected ActionResult<PagedResponse<T>> PagedSuccess<T>(T data, int totalCount, int pageNumber, int pageSize, string message = "Data retrieved successfully")
    {
        return Ok(PagedResponse<T>.SuccesResponse(data, totalCount, pageNumber, pageSize, message));
    }

    /// <summary>
    /// Return bad request response
    /// </summary>
    protected ActionResult<ApiResponse<T>> BadRequest<T>(string message = "Bad request")
    {
        return BadRequest(ApiResponse<T>.BadRequestResult(message));
    }

    /// <summary>
    /// Return bad request response with validation errors
    /// </summary>
    protected ActionResult<ApiResponse<T>> BadRequest<T>(List<string> errors, string message = "Validation failed")
    {
        return BadRequest(ApiResponse<T>.ErrorResult(errors, message, ApiResponseStatus.ValidationError));
    }

    /// <summary>
    /// Return not found response
    /// </summary>
    protected ActionResult<ApiResponse<T>> NotFound<T>(string message = "Resource not found")
    {
        return NotFound(ApiResponse<T>.NotFoundResult(message));
    }

    /// <summary>
    /// Return unauthorized response
    /// </summary>
    protected ActionResult<ApiResponse<T>> Unauthorized<T>(string message = "Unauthorized access")
    {
        return Unauthorized(ApiResponse<T>.UnauthorizedResult(message));
    }

    /// <summary>
    /// Return forbidden response
    /// </summary>
    protected ActionResult<ApiResponse<T>> Forbidden<T>(string message = "Access forbidden")
    {
        return StatusCode(403, ApiResponse<T>.ErrorResult(message, ApiResponseStatus.Forbidden));
    }

    /// <summary>
    /// Return internal server error response
    /// </summary>
    protected ActionResult<ApiResponse<T>> InternalServerError<T>(string message = "Internal server error")
    {
        return StatusCode(500, ApiResponse<T>.ErrorResult(message, ApiResponseStatus.InternalServerError));
    }

    /// <summary>
    /// Return conflict response
    /// </summary>
    protected ActionResult<ApiResponse<T>> Conflict<T>(string message = "Resource conflict")
    {
        return Conflict(ApiResponse<T>.ErrorResult(message, ApiResponseStatus.Conflict));
    }

    /// <summary>
    /// Handle service response and return appropriate HTTP response
    /// </summary>
    protected ActionResult<ApiResponse<T>> HandleServiceResponse<T>(ApiResponse<T> serviceResponse)
    {
        return serviceResponse.StatusCode switch
        {
            ApiResponseStatus.Success => Ok(serviceResponse),
            ApiResponseStatus.Created => Created("", serviceResponse),
            ApiResponseStatus.BadRequest => BadRequest(serviceResponse),
            ApiResponseStatus.Unauthorized => Unauthorized(serviceResponse),
            ApiResponseStatus.Forbidden => StatusCode(403, serviceResponse),
            ApiResponseStatus.NotFound => NotFound(serviceResponse),
            ApiResponseStatus.Conflict => Conflict(serviceResponse),
            ApiResponseStatus.ValidationError => BadRequest(serviceResponse),
            ApiResponseStatus.InternalServerError => StatusCode(500, serviceResponse),
            _ => StatusCode(500, serviceResponse)
        };
    }

    /// <summary>
    /// Handle paged service response
    /// </summary>
    protected ActionResult HandlePagedServiceResponse<T>(PagedResponse<T> serviceResponse)
    {
        return serviceResponse.StatusCode switch
        {
            ApiResponseStatus.Success => Ok(serviceResponse),
            ApiResponseStatus.BadRequest => BadRequest(serviceResponse),
            ApiResponseStatus.Unauthorized => Unauthorized(serviceResponse),
            ApiResponseStatus.Forbidden => StatusCode(403, serviceResponse),
            ApiResponseStatus.NotFound => NotFound(serviceResponse),
            ApiResponseStatus.InternalServerError => StatusCode(500, serviceResponse),
            _ => StatusCode(500, serviceResponse)
        };
    }

    /// <summary>
    /// Validate model state and return validation errors if any
    /// </summary>
    protected List<string>? GetValidationErrors()
    {
        if (!ModelState.IsValid)
        {
            return ModelState
                .SelectMany(x => x.Value?.Errors ?? new Microsoft.AspNetCore.Mvc.ModelBinding.ModelErrorCollection())
                .Select(x => x.ErrorMessage)
                .ToList();
        }
        return null;
    }

    /// <summary>
    /// Get client IP address
    /// </summary>
    protected string? GetClientIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    /// <summary>
    /// Get user agent from request headers
    /// </summary>
    protected string? GetUserAgent()
    {
        return HttpContext.Request.Headers["User-Agent"].FirstOrDefault();
    }

    /// <summary>
    /// Get request ID for tracking
    /// </summary>
    protected string GetRequestId()
    {
        return HttpContext.TraceIdentifier;
    }

    /// <summary>
    /// Check if user is authenticated
    /// </summary>
    protected bool IsAuthenticated => User.Identity?.IsAuthenticated ?? false;

    /// <summary>
    /// Require authentication - throws UnauthorizedAccessException if not authenticated
    /// </summary>
    protected void RequireAuthentication()
    {
        if (!IsAuthenticated)
        {
            throw new UnauthorizedAccessException("Authentication required");
        }
    }

    /// <summary>
    /// Require specific role - throws UnauthorizedAccessException if user doesn't have role
    /// </summary>
    protected void RequireRole(string role)
    {
        RequireAuthentication();
        if (!HasRole(role))
        {
            throw new UnauthorizedAccessException($"Role '{role}' required");
        }
    }

    /// <summary>
    /// Require specific permission - throws UnauthorizedAccessException if user doesn't have permission
    /// </summary>
    protected void RequirePermission(string permission)
    {
        RequireAuthentication();
        if (!HasPermission(permission))
        {
            throw new UnauthorizedAccessException($"Permission '{permission}' required");
        }
    }
}