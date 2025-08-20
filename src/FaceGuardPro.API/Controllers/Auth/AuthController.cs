using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FaceGuardPro.Core.Interfaces;
using FaceGuardPro.Shared.Models;

namespace FaceGuardPro.API.Controllers;

/// <summary>
/// Authentication and authorization endpoints
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : BaseController
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthenticationService authenticationService,
        ILogger<AuthController> logger)
    {
        _authenticationService = authenticationService;
        _logger = logger;
    }

    /// <summary>
    /// Login with username and password
    /// </summary>
    /// <param name="loginDto">Login credentials</param>
    /// <returns>Authentication result with JWT token</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthenticationResultDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<ActionResult<ApiResponse<AuthenticationResultDto>>> Login(
        [FromBody] LoginDto loginDto)
    {
        try
        {
            var validationErrors = GetValidationErrors();
            if (validationErrors != null)
            {
                return BadRequest<AuthenticationResultDto>(validationErrors);
            }

            var result = await _authenticationService.AuthenticateWithCredentialsAsync(loginDto);

            if (result.Success)
            {
                _logger.LogInformation("User logged in successfully: {Username} from IP: {IpAddress}",
                    loginDto.Username, GetClientIpAddress());
            }
            else
            {
                _logger.LogWarning("Failed login attempt for username: {Username} from IP: {IpAddress}",
                    loginDto.Username, GetClientIpAddress());
            }

            return HandleServiceResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for username: {Username}", loginDto.Username);
            return InternalServerError<AuthenticationResultDto>("Authentication error occurred");
        }
    }

    /// <summary>
    /// Authenticate with face recognition
    /// </summary>
    /// <param name="faceAuthDto">Face authentication data</param>
    /// <returns>Authentication result</returns>
    [HttpPost("authenticate-face")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthenticationResultDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<ActionResult<ApiResponse<AuthenticationResultDto>>> AuthenticateWithFace(
        [FromBody] FaceAuthenticationDto faceAuthDto)
    {
        try
        {
            var validationErrors = GetValidationErrors();
            if (validationErrors != null)
            {
                return BadRequest<AuthenticationResultDto>(validationErrors);
            }

            var result = await _authenticationService.AuthenticateWithFaceAsync(faceAuthDto);

            if (result.Success)
            {
                _logger.LogInformation("Face authentication successful for employee: {EmployeeId} from IP: {IpAddress}",
                    faceAuthDto.EmployeeId, GetClientIpAddress());
            }
            else
            {
                _logger.LogWarning("Face authentication failed for employee: {EmployeeId} from IP: {IpAddress}. Reason: {Reason}",
                    faceAuthDto.EmployeeId, GetClientIpAddress(), result.Message);
            }

            return HandleServiceResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during face authentication for employee: {EmployeeId}", faceAuthDto.EmployeeId);
            return InternalServerError<AuthenticationResultDto>("Face authentication error occurred");
        }
    }

    /// <summary>
    /// Refresh JWT token
    /// </summary>
    /// <param name="refreshTokenDto">Refresh token data</param>
    /// <returns>New JWT token</returns>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<TokenDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<ActionResult<ApiResponse<TokenDto>>> RefreshToken(
        [FromBody] RefreshTokenDto refreshTokenDto)
    {
        try
        {
            var validationErrors = GetValidationErrors();
            if (validationErrors != null)
            {
                return BadRequest<TokenDto>(validationErrors);
            }

            var result = await _authenticationService.RefreshTokenAsync(refreshTokenDto);

            if (result.Success)
            {
                _logger.LogInformation("Token refreshed successfully from IP: {IpAddress}", GetClientIpAddress());
            }

            return HandleServiceResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return InternalServerError<TokenDto>("Token refresh error occurred");
        }
    }

    /// <summary>
    /// Logout - revoke refresh token
    /// </summary>
    /// <param name="refreshTokenDto">Refresh token to revoke</param>
    /// <returns>Success status</returns>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<ActionResult<ApiResponse<bool>>> Logout(
        [FromBody] RefreshTokenDto refreshTokenDto)
    {
        try
        {
            var result = await _authenticationService.RevokeTokenAsync(refreshTokenDto.RefreshToken);

            if (result.Success)
            {
                _logger.LogInformation("User logged out: {UserId} from IP: {IpAddress}",
                    CurrentUserId, GetClientIpAddress());
            }

            return HandleServiceResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout for user: {UserId}", CurrentUserId);
            return InternalServerError<bool>("Logout error occurred");
        }
    }

    /// <summary>
    /// Get current user profile
    /// </summary>
    /// <returns>Current user information</returns>
    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetProfile()
    {
        try
        {
            RequireAuthentication();

            var result = await _authenticationService.GetUserProfileAsync(CurrentUserId!.Value);
            return HandleServiceResponse(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized<UserDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving profile for user: {UserId}", CurrentUserId);
            return InternalServerError<UserDto>("Error retrieving user profile");
        }
    }

    /// <summary>
    /// Change password
    /// </summary>
    /// <param name="changePasswordDto">Password change data</param>
    /// <returns>Success status</returns>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<ActionResult<ApiResponse<bool>>> ChangePassword(
        [FromBody] ChangePasswordDto changePasswordDto)
    {
        try
        {
            RequireAuthentication();

            var validationErrors = GetValidationErrors();
            if (validationErrors != null)
            {
                return BadRequest<bool>(validationErrors);
            }

            var result = await _authenticationService.ChangePasswordAsync(CurrentUserId!.Value, changePasswordDto);

            if (result.Success)
            {
                _logger.LogInformation("Password changed for user: {UserId}", CurrentUserId);
            }

            return HandleServiceResponse(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized<bool>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user: {UserId}", CurrentUserId);
            return InternalServerError<bool>("Error changing password");
        }
    }

    /// <summary>
    /// Register new user (Admin only)
    /// </summary>
    /// <param name="registerUserDto">User registration data</param>
    /// <returns>Created user</returns>
    [HttpPost("register")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    public async Task<ActionResult<ApiResponse<UserDto>>> Register(
        [FromBody] RegisterUserDto registerUserDto)
    {
        try
        {
            RequireRole("Admin");

            var validationErrors = GetValidationErrors();
            if (validationErrors != null)
            {
                return BadRequest<UserDto>(validationErrors);
            }

            var result = await _authenticationService.RegisterUserAsync(registerUserDto);

            if (result.Success)
            {
                _logger.LogInformation("New user registered: {Username} by admin: {AdminId}",
                    registerUserDto.Username, CurrentUserId);
                return Created($"api/v1/auth/users/{result.Data?.Id}", result);
            }

            return HandleServiceResponse(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbidden<UserDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user: {Username}", registerUserDto.Username);
            return InternalServerError<UserDto>("Error registering user");
        }
    }

    /// <summary>
    /// Get user roles
    /// </summary>
    /// <returns>List of user roles</returns>
    [HttpGet("roles")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<string>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<ActionResult<ApiResponse<IEnumerable<string>>>> GetUserRoles()
    {
        try
        {
            RequireAuthentication();

            var result = await _authenticationService.GetUserRolesAsync(CurrentUserId!.Value);
            return HandleServiceResponse(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized<IEnumerable<string>>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving roles for user: {UserId}", CurrentUserId);
            return InternalServerError<IEnumerable<string>>("Error retrieving user roles");
        }
    }

    /// <summary>
    /// Get user permissions
    /// </summary>
    /// <returns>List of user permissions</returns>
    [HttpGet("permissions")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<string>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<ActionResult<ApiResponse<IEnumerable<string>>>> GetUserPermissions()
    {
        try
        {
            RequireAuthentication();

            var result = await _authenticationService.GetUserPermissionsAsync(CurrentUserId!.Value);
            return HandleServiceResponse(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized<IEnumerable<string>>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving permissions for user: {UserId}", CurrentUserId);
            return InternalServerError<IEnumerable<string>>("Error retrieving user permissions");
        }
    }

    /// <summary>
    /// Validate token (for testing purposes)
    /// </summary>
    /// <returns>Token validation result</returns>
    [HttpGet("validate-token")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserClaimsDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<ActionResult<ApiResponse<UserClaimsDto>>> ValidateToken()
    {
        try
        {
            RequireAuthentication();

            var userClaims = new UserClaimsDto
            {
                UserId = CurrentUserId?.ToString() ?? "",
                Username = CurrentUsername ?? "",
                Email = CurrentUserEmail ?? "",
                Roles = CurrentUserRoles.ToList(),
                Permissions = User.FindAll("permission").Select(c => c.Value).ToList(),
                TokenIssuedAt = DateTime.UtcNow, // This should come from JWT claims in real implementation
                TokenExpiry = DateTime.UtcNow.AddHours(1) // This should come from JWT claims in real implementation
            };

            return Success(userClaims, "Token is valid");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized<UserClaimsDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token for user: {UserId}", CurrentUserId);
            return InternalServerError<UserClaimsDto>("Error validating token");
        }
    }
}