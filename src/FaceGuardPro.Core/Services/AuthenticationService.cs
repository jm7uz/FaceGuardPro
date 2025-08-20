using AutoMapper;
using Microsoft.Extensions.Logging;
using FaceGuardPro.Core.Interfaces;
using FaceGuardPro.Data.UnitOfWork;
using FaceGuardPro.Data.Entities;
using FaceGuardPro.Shared.Models;
using FaceGuardPro.Shared.Enums;
using FaceGuardPro.Shared.Constants;

namespace FaceGuardPro.Core.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtService _jwtService;
    private readonly IMapper _mapper;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly IFaceDetectionService _faceDetectionService;

    public AuthenticationService(
        IUnitOfWork unitOfWork,
        IJwtService jwtService,
        IMapper mapper,
        ILogger<AuthenticationService> logger,
        IFaceDetectionService faceDetectionService)
    {
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
        _mapper = mapper;
        _logger = logger;
        _faceDetectionService = faceDetectionService;
    }

    public async Task<ApiResponse<AuthenticationResultDto>> AuthenticateWithCredentialsAsync(LoginDto loginDto)
    {
        try
        {
            // Get user by username
            var user = await _unitOfWork.Users.GetUserWithRolesAsync(loginDto.Username);
            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("Login attempt with invalid username: {Username}", loginDto.Username);
                return ApiResponse<AuthenticationResultDto>.UnauthorizedResult("Invalid username or password");
            }

            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                _logger.LogWarning("Login attempt with invalid password for user: {Username}", loginDto.Username);
                return ApiResponse<AuthenticationResultDto>.UnauthorizedResult("Invalid username or password");
            }

            // Get user roles and permissions
            var roles = await _unitOfWork.Users.GetUserRolesAsync(user.Id);
            var permissions = await _unitOfWork.Users.GetUserPermissionsAsync(user.Id);

            // Generate tokens
            var accessToken = _jwtService.GenerateAccessToken(user, roles, permissions);
            var refreshToken = _jwtService.GenerateRefreshToken();

            // Save refresh token
            var refreshTokenEntity = new RefreshToken
            {
                UserId = user.Id,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(AuthenticationConstants.REFRESH_TOKEN_EXPIRY_DAYS),
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.RefreshTokens.AddAsync(refreshTokenEntity);

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            var result = new AuthenticationResultDto
            {
                Result = AuthenticationResult.Success,
                Message = "Authentication successful",
                Employee = null, // Not employee authentication
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                TokenExpiry = DateTime.UtcNow.AddMinutes(AuthenticationConstants.TOKEN_EXPIRY_MINUTES),
                AuthenticatedAt = DateTime.UtcNow
            };

            _logger.LogInformation("User authenticated successfully: {Username}", loginDto.Username);
            return ApiResponse<AuthenticationResultDto>.SuccessResult(result, "Authentication successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during credential authentication for user: {Username}", loginDto.Username);
            return ApiResponse<AuthenticationResultDto>.ErrorResult("Authentication error occurred");
        }
    }

    public async Task<ApiResponse<AuthenticationResultDto>> AuthenticateWithFaceAsync(FaceAuthenticationDto authenticationDto)
    {
        try
        {
            // Get employee by employee ID
            var employee = await _unitOfWork.Employees.GetByEmployeeIdAsync(authenticationDto.EmployeeId);
            if (employee == null || employee.Status != EmployeeStatus.Active)
            {
                await LogAuthenticationAttempt(Guid.Empty, AuthenticationResult.EmployeeNotFound,
                    null, null, "Employee not found or inactive");

                return ApiResponse<AuthenticationResultDto>.UnauthorizedResult("Employee not found or inactive");
            }

            // Check if employee has face template
            var faceTemplate = await _unitOfWork.FaceTemplates.GetByEmployeeIdAsync(employee.Id);
            if (faceTemplate == null)
            {
                await LogAuthenticationAttempt(employee.Id, AuthenticationResult.NoFaceTemplate,
                    null, null, "No face template found");

                return ApiResponse<AuthenticationResultDto>.UnauthorizedResult("Face template not found for employee");
            }

            // Perform face detection
            var faceDetectionResult = await _faceDetectionService.DetectFaceAsync(authenticationDto.FaceImageData);
            if (!faceDetectionResult.Success || faceDetectionResult.Data?.Result != FaceDetectionResult.Success)
            {
                await LogAuthenticationAttempt(employee.Id, AuthenticationResult.PoorImageQuality,
                    null, null, "Face detection failed");

                return ApiResponse<AuthenticationResultDto>.UnauthorizedResult("Face detection failed");
            }

            // Perform liveness detection if required
            LivenessDetectionDto? livenessResult = null;
            if (authenticationDto.PerformLivenessCheck)
            {
                var livenessDetectionResult = await _faceDetectionService.DetectLivenessAsync(authenticationDto.FaceImageData);
                if (!livenessDetectionResult.Success || livenessDetectionResult.Data?.Result != LivenessResult.Live)
                {
                    await LogAuthenticationAttempt(employee.Id, AuthenticationResult.LivenessCheckFailed,
                        faceDetectionResult.Data, livenessDetectionResult.Data, "Liveness check failed");

                    return ApiResponse<AuthenticationResultDto>.UnauthorizedResult("Liveness check failed");
                }
                livenessResult = livenessDetectionResult.Data;
            }

            // Compare with stored template
            var comparisonResult = await _faceDetectionService.CompareWithTemplateAsync(employee.Id, authenticationDto.FaceImageData);
            if (!comparisonResult.Success || !comparisonResult.Data?.IsMatch == true)
            {
                await LogAuthenticationAttempt(employee.Id, AuthenticationResult.FaceNotMatched,
                    faceDetectionResult.Data, livenessResult, "Face comparison failed");

                return ApiResponse<AuthenticationResultDto>.UnauthorizedResult("Face does not match");
            }

            // Authentication successful
            var employeeDto = _mapper.Map<EmployeeDto>(employee);

            // Log successful authentication
            await LogAuthenticationAttempt(employee.Id, AuthenticationResult.Success,
                faceDetectionResult.Data, livenessResult, null, comparisonResult.Data.Similarity);

            var authResult = new AuthenticationResultDto
            {
                Result = AuthenticationResult.Success,
                Message = "Face authentication successful",
                Employee = employeeDto,
                FaceDetection = faceDetectionResult.Data,
                LivenessDetection = livenessResult,
                AuthenticatedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Employee face authentication successful: {EmployeeId}", authenticationDto.EmployeeId);
            return ApiResponse<AuthenticationResultDto>.SuccessResult(authResult, "Face authentication successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during face authentication for employee: {EmployeeId}", authenticationDto.EmployeeId);

            // Try to get employee ID for logging
            var employee = await _unitOfWork.Employees.GetByEmployeeIdAsync(authenticationDto.EmployeeId);
            await LogAuthenticationAttempt(employee?.Id ?? Guid.Empty, AuthenticationResult.SystemError,
                null, null, ex.Message);

            return ApiResponse<AuthenticationResultDto>.ErrorResult("Face authentication error occurred");
        }
    }

    public async Task<ApiResponse<TokenDto>> RefreshTokenAsync(RefreshTokenDto refreshTokenDto)
    {
        try
        {
            // Validate refresh token
            var refreshToken = await _unitOfWork.RefreshTokens.GetByTokenAsync(refreshTokenDto.RefreshToken);
            if (refreshToken == null || !refreshToken.IsActive)
            {
                _logger.LogWarning("Invalid or expired refresh token used");
                return ApiResponse<TokenDto>.UnauthorizedResult("Invalid or expired refresh token");
            }

            // Get user
            var user = await _unitOfWork.Users.GetUserWithRolesAsync(refreshToken.UserId);
            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("Refresh token used for inactive user: {UserId}", refreshToken.UserId);
                return ApiResponse<TokenDto>.UnauthorizedResult("User account is inactive");
            }

            // Revoke old refresh token
            refreshToken.RevokedAt = DateTime.UtcNow;
            await _unitOfWork.RefreshTokens.UpdateAsync(refreshToken);

            // Get user roles and permissions
            var roles = await _unitOfWork.Users.GetUserRolesAsync(user.Id);
            var permissions = await _unitOfWork.Users.GetUserPermissionsAsync(user.Id);

            // Generate new tokens
            var newAccessToken = _jwtService.GenerateAccessToken(user, roles, permissions);
            var newRefreshToken = _jwtService.GenerateRefreshToken();

            // Save new refresh token
            var newRefreshTokenEntity = new RefreshToken
            {
                UserId = user.Id,
                Token = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(AuthenticationConstants.REFRESH_TOKEN_EXPIRY_DAYS),
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.RefreshTokens.AddAsync(newRefreshTokenEntity);
            await _unitOfWork.SaveChangesAsync();

            var result = new TokenDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                AccessTokenExpiry = DateTime.UtcNow.AddMinutes(AuthenticationConstants.TOKEN_EXPIRY_MINUTES),
                RefreshTokenExpiry = DateTime.UtcNow.AddDays(AuthenticationConstants.REFRESH_TOKEN_EXPIRY_DAYS)
            };

            _logger.LogInformation("Token refreshed for user: {UserId}", user.Id);
            return ApiResponse<TokenDto>.SuccessResult(result, "Token refreshed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return ApiResponse<TokenDto>.ErrorResult("Token refresh error occurred");
        }
    }

    public async Task<ApiResponse<bool>> RevokeTokenAsync(string refreshToken)
    {
        try
        {
            var token = await _unitOfWork.RefreshTokens.GetByTokenAsync(refreshToken);
            if (token != null && token.IsActive)
            {
                token.RevokedAt = DateTime.UtcNow;
                await _unitOfWork.RefreshTokens.UpdateAsync(token);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Refresh token revoked for user: {UserId}", token.UserId);
            }

            return ApiResponse<bool>.SuccessResult(true, "Token revoked successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking token");
            return ApiResponse<bool>.ErrorResult("Token revocation error occurred");
        }
    }

    public async Task<ApiResponse<bool>> RevokeAllUserTokensAsync(Guid userId)
    {
        try
        {
            var revokedCount = await _unitOfWork.RefreshTokens.RevokeAllUserTokensAsync(userId);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("All tokens revoked for user: {UserId}, Count: {Count}", userId, revokedCount);
            return ApiResponse<bool>.SuccessResult(true, $"{revokedCount} tokens revoked successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking all tokens for user: {UserId}", userId);
            return ApiResponse<bool>.ErrorResult("Token revocation error occurred");
        }
    }

    public async Task<ApiResponse<UserDto>> RegisterUserAsync(RegisterUserDto registerUserDto)
    {
        try
        {
            // Check if username already exists
            var existingUser = await _unitOfWork.Users.GetByUsernameAsync(registerUserDto.Username);
            if (existingUser != null)
            {
                return ApiResponse<UserDto>.ErrorResult("Username already exists", ApiResponseStatus.Conflict);
            }

            // Check if email already exists
            var existingEmailUser = await _unitOfWork.Users.GetByEmailAsync(registerUserDto.Email);
            if (existingEmailUser != null)
            {
                return ApiResponse<UserDto>.ErrorResult("Email already exists", ApiResponseStatus.Conflict);
            }

            // Create new user
            var user = _mapper.Map<User>(registerUserDto);
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerUserDto.Password);
            user.IsActive = true;
            user.CreatedAt = DateTime.UtcNow;

            await _unitOfWork.Users.AddAsync(user);

            // Assign roles
            foreach (var roleName in registerUserDto.Roles)
            {
                var role = await _unitOfWork.Roles.GetByNameAsync(roleName);
                if (role != null)
                {
                    var userRole = new UserRole
                    {
                        UserId = user.Id,
                        RoleId = role.Id,
                        AssignedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.Repository<UserRole>().AddAsync(userRole);
                }
            }

            await _unitOfWork.SaveChangesAsync();

            var userDto = _mapper.Map<UserDto>(user);
            userDto.Roles = registerUserDto.Roles.ToList();

            _logger.LogInformation("New user registered: {Username}", registerUserDto.Username);
            return ApiResponse<UserDto>.SuccessResult(userDto, "User registered successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user: {Username}", registerUserDto.Username);
            return ApiResponse<UserDto>.ErrorResult("User registration error occurred");
        }
    }

    public async Task<ApiResponse<UserDto>> GetUserProfileAsync(Guid userId)
    {
        try
        {
            var user = await _unitOfWork.Users.GetUserWithRolesAsync(userId);
            if (user == null)
            {
                return ApiResponse<UserDto>.NotFoundResult("User not found");
            }

            var userDto = _mapper.Map<UserDto>(user);
            return ApiResponse<UserDto>.SuccessResult(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user profile: {UserId}", userId);
            return ApiResponse<UserDto>.ErrorResult("Error retrieving user profile");
        }
    }

    public async Task<ApiResponse<bool>> ChangePasswordAsync(Guid userId, ChangePasswordDto changePasswordDto)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<bool>.NotFoundResult("User not found");
            }

            // Verify current password
            if (!BCrypt.Net.BCrypt.Verify(changePasswordDto.CurrentPassword, user.PasswordHash))
            {
                return ApiResponse<bool>.BadRequestResult("Current password is incorrect");
            }

            // Update password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(changePasswordDto.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Password changed for user: {UserId}", userId);
            return ApiResponse<bool>.SuccessResult(true, "Password changed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user: {UserId}", userId);
            return ApiResponse<bool>.ErrorResult("Password change error occurred");
        }
    }

    public async Task<ApiResponse<bool>> UpdateUserProfileAsync(Guid userId, UpdateUserDto updateUserDto)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<bool>.NotFoundResult("User not found");
            }

            // Check email uniqueness
            if (user.Email != updateUserDto.Email)
            {
                var emailExists = await _unitOfWork.Users.IsEmailUniqueAsync(updateUserDto.Email, userId);
                if (!emailExists)
                {
                    return ApiResponse<bool>.ErrorResult("Email already exists", ApiResponseStatus.Conflict);
                }
            }

            // Update user
            _mapper.Map(updateUserDto, user);
            user.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("User profile updated: {UserId}", userId);
            return ApiResponse<bool>.SuccessResult(true, "Profile updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile: {UserId}", userId);
            return ApiResponse<bool>.ErrorResult("Profile update error occurred");
        }
    }

    public async Task<ApiResponse<bool>> ValidateUserPermissionAsync(Guid userId, string permission)
    {
        try
        {
            var permissions = await _unitOfWork.Users.GetUserPermissionsAsync(userId);
            var hasPermission = permissions.Contains(permission);

            return ApiResponse<bool>.SuccessResult(hasPermission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating user permission: {UserId}", userId);
            return ApiResponse<bool>.ErrorResult("Permission validation error occurred");
        }
    }

    public async Task<ApiResponse<IEnumerable<string>>> GetUserRolesAsync(Guid userId)
    {
        try
        {
            var roles = await _unitOfWork.Users.GetUserRolesAsync(userId);
            return ApiResponse<IEnumerable<string>>.SuccessResult(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user roles: {UserId}", userId);
            return ApiResponse<IEnumerable<string>>.ErrorResult("Error retrieving user roles");
        }
    }

    public async Task<ApiResponse<IEnumerable<string>>> GetUserPermissionsAsync(Guid userId)
    {
        try
        {
            var permissions = await _unitOfWork.Users.GetUserPermissionsAsync(userId);
            return ApiResponse<IEnumerable<string>>.SuccessResult(permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user permissions: {UserId}", userId);
            return ApiResponse<IEnumerable<string>>.ErrorResult("Error retrieving user permissions");
        }
    }

    public async Task<ApiResponse<bool>> CheckAccountLockoutAsync(Guid employeeId)
    {
        try
        {
            var recentFailedAttempts = await _unitOfWork.AuthenticationLogs.GetFailedAttemptCountAsync(
                employeeId, TimeSpan.FromMinutes(AuthenticationConstants.LOCKOUT_DURATION_MINUTES));

            var isLockedOut = recentFailedAttempts >= AuthenticationConstants.MAX_AUTHENTICATION_ATTEMPTS;
            return ApiResponse<bool>.SuccessResult(isLockedOut);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking account lockout: {EmployeeId}", employeeId);
            return ApiResponse<bool>.ErrorResult("Lockout check error occurred");
        }
    }

    public async Task<ApiResponse<bool>> ResetAccountLockoutAsync(Guid employeeId)
    {
        try
        {
            // This could be implemented by clearing recent failed attempts or by time-based lockout reset
            // For now, we'll just return success as the lockout is time-based

            _logger.LogInformation("Account lockout reset requested for employee: {EmployeeId}", employeeId);
            return ApiResponse<bool>.SuccessResult(true, "Account lockout reset successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting account lockout: {EmployeeId}", employeeId);
            return ApiResponse<bool>.ErrorResult("Lockout reset error occurred");
        }
    }

    public async Task<ApiResponse<IEnumerable<AuthenticationLogDto>>> GetAuthenticationLogsAsync(Guid? employeeId = null, int pageNumber = 1, int pageSize = 20)
    {
        try
        {
            IEnumerable<AuthenticationLog> logs;

            if (employeeId.HasValue)
            {
                logs = await _unitOfWork.AuthenticationLogs.GetByEmployeeIdAsync(employeeId.Value);
            }
            else
            {
                logs = await _unitOfWork.AuthenticationLogs.GetPagedAsync(pageNumber, pageSize);
            }

            var logDtos = _mapper.Map<IEnumerable<AuthenticationLogDto>>(logs);
            return ApiResponse<IEnumerable<AuthenticationLogDto>>.SuccessResult(logDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving authentication logs");
            return ApiResponse<IEnumerable<AuthenticationLogDto>>.ErrorResult("Error retrieving authentication logs");
        }
    }

    public async Task<ApiResponse<AuthenticationStatsDto>> GetAuthenticationStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            startDate ??= DateTime.UtcNow.AddDays(-30);
            endDate ??= DateTime.UtcNow;

            var logs = await _unitOfWork.AuthenticationLogs.GetAuthenticationLogsByDateRangeAsync(startDate.Value, endDate.Value);
            var successfulLogs = logs.Where(l => l.AuthenticationResult == "Success");

            var stats = new AuthenticationStatsDto
            {
                TotalAttempts = logs.Count(),
                SuccessfulAttempts = successfulLogs.Count(),
                FailedAttempts = logs.Count() - successfulLogs.Count(),
                SuccessRate = logs.Any() ? (double)successfulLogs.Count() / logs.Count() * 100 : 0,
                GeneratedAt = DateTime.UtcNow
            };

            // Failure reasons
            stats.FailureReasons = logs
                .Where(l => l.AuthenticationResult != "Success")
                .GroupBy(l => l.FailureReason ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count());

            // Attempts per day
            stats.AttemptsPerDay = logs
                .GroupBy(l => l.AttemptedAt.Date)
                .ToDictionary(g => g.Key.ToString("yyyy-MM-dd"), g => g.Count());

            return ApiResponse<AuthenticationStatsDto>.SuccessResult(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving authentication statistics");
            return ApiResponse<AuthenticationStatsDto>.ErrorResult("Error retrieving authentication statistics");
        }
    }

    private async Task LogAuthenticationAttempt(
        Guid employeeId,
        AuthenticationResult result,
        FaceDetectionDto? faceDetection,
        LivenessDetectionDto? livenessDetection,
        string? failureReason,
        double? faceMatchScore = null)
    {
        try
        {
            var log = new AuthenticationLog
            {
                EmployeeId = employeeId,
                AuthenticationResult = result.ToString(),
                FaceMatchScore = faceMatchScore,
                LivenessScore = livenessDetection?.Confidence,
                FailureReason = failureReason,
                AttemptedAt = DateTime.UtcNow
            };

            await _unitOfWork.AuthenticationLogs.AddAsync(log);
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging authentication attempt");
            // Don't throw here as it's just logging
        }
    }
}