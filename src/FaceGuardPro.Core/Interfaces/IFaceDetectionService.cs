using FaceGuardPro.Shared.Models;

namespace FaceGuardPro.Core.Interfaces;

public interface IFaceDetectionService
{
    // Face Detection
    Task<ApiResponse<FaceDetectionDto>> DetectFaceAsync(byte[] imageData);
    Task<ApiResponse<IEnumerable<FaceDetectionDto>>> DetectMultipleFacesAsync(byte[] imageData);
    Task<ApiResponse<FaceQualityMetrics>> AnalyzeFaceQualityAsync(byte[] imageData);

    // Liveness Detection (moved here from ILivenessDetectionService for simplicity)
    Task<ApiResponse<LivenessDetectionDto>> DetectLivenessAsync(byte[] imageData);
    Task<ApiResponse<LivenessDetectionDto>> DetectLivenessFromStreamAsync(IEnumerable<byte[]> frameData);

    // Face Template Management
    Task<ApiResponse<FaceTemplateDto>> CreateFaceTemplateAsync(CreateFaceTemplateDto createTemplateDto);
    Task<ApiResponse<FaceTemplateDto>> GetFaceTemplateAsync(Guid employeeId);
    Task<ApiResponse<bool>> UpdateFaceTemplateAsync(Guid employeeId, byte[] newImageData);
    Task<ApiResponse<bool>> DeleteFaceTemplateAsync(Guid employeeId);

    // Face Comparison
    Task<ApiResponse<FaceComparisonResult>> CompareFacesAsync(byte[] image1Data, byte[] image2Data);
    Task<ApiResponse<FaceComparisonResult>> CompareWithTemplateAsync(Guid employeeId, byte[] imageData);
    Task<ApiResponse<IEnumerable<FaceComparisonResult>>> FindSimilarFacesAsync(byte[] imageData, double threshold = 0.8);

    // Face Recognition
    Task<ApiResponse<EmployeeDto>> RecognizeFaceAsync(byte[] imageData);
    Task<ApiResponse<IEnumerable<EmployeeDto>>> RecognizeMultipleFacesAsync(byte[] imageData);

    // Template Operations
    Task<ApiResponse<IEnumerable<FaceTemplateDto>>> GetAllFaceTemplatesAsync();
    Task<ApiResponse<IEnumerable<FaceTemplateDto>>> GetHighQualityTemplatesAsync(double minQuality = 0.8);
    Task<ApiResponse<bool>> ValidateTemplateQualityAsync(byte[] templateData);

    // Batch Operations
    Task<ApiResponse<BatchFaceProcessingResult>> ProcessMultipleImagesAsync(IEnumerable<BatchImageInput> images);
    Task<ApiResponse<int>> RegenerateLowQualityTemplatesAsync(double qualityThreshold = 0.6);
}

public interface ILivenessDetectionService
{
    // Basic Liveness Detection
    Task<ApiResponse<LivenessDetectionDto>> DetectLivenessAsync(byte[] imageData);
    Task<ApiResponse<LivenessDetectionDto>> DetectLivenessFromStreamAsync(IEnumerable<byte[]> frameData);

    // Blink Detection
    Task<ApiResponse<BlinkDetectionDto>> DetectBlinkAsync(byte[] imageData);
    Task<ApiResponse<bool>> ValidateBlinkSequenceAsync(IEnumerable<byte[]> frameSequence);

    // Head Pose Detection
    Task<ApiResponse<HeadPoseDto>> DetectHeadPoseAsync(byte[] imageData);
    Task<ApiResponse<bool>> ValidateHeadMovementAsync(IEnumerable<byte[]> frameSequence, double movementThreshold = 15.0);

    // Challenge System
    Task<ApiResponse<ChallengeDto>> CreateChallengeAsync(CreateChallengeDto createChallengeDto);
    Task<ApiResponse<ChallengeDto>> ProcessChallengeResponseAsync(ChallengeResponseDto challengeResponse);
    Task<ApiResponse<LivenessSessionDto>> StartLivenessSessionAsync();
    Task<ApiResponse<LivenessSessionDto>> CompleteLivenessSessionAsync(Guid sessionId);
    Task<ApiResponse<ChallengeDto>> GetActiveChallengeAsync(Guid sessionId);

    // Anti-Spoofing
    Task<ApiResponse<bool>> DetectPhotoSpoofingAsync(byte[] imageData);
    Task<ApiResponse<bool>> DetectVideoReplayAsync(IEnumerable<byte[]> frameSequence);
    Task<ApiResponse<bool>> DetectPrintAttackAsync(byte[] imageData);
    Task<ApiResponse<bool>> DetectDigitalDisplayAsync(byte[] imageData);

    // Advanced Features
    Task<ApiResponse<bool>> ValidateEyeMovementAsync(IEnumerable<byte[]> frameSequence);
    Task<ApiResponse<bool>> DetectMouthMovementAsync(IEnumerable<byte[]> frameSequence);
    Task<ApiResponse<double>> CalculateTextureScoreAsync(byte[] imageData);
}

public interface IAuthenticationService
{
    // Main Authentication
    Task<ApiResponse<AuthenticationResultDto>> AuthenticateWithFaceAsync(FaceAuthenticationDto authenticationDto);
    Task<ApiResponse<AuthenticationResultDto>> AuthenticateWithCredentialsAsync(LoginDto loginDto);

    // Token Management
    Task<ApiResponse<TokenDto>> RefreshTokenAsync(RefreshTokenDto refreshTokenDto);
    Task<ApiResponse<bool>> RevokeTokenAsync(string refreshToken);
    Task<ApiResponse<bool>> RevokeAllUserTokensAsync(Guid userId);

    // User Management
    Task<ApiResponse<UserDto>> RegisterUserAsync(RegisterUserDto registerUserDto);
    Task<ApiResponse<UserDto>> GetUserProfileAsync(Guid userId);
    Task<ApiResponse<bool>> ChangePasswordAsync(Guid userId, ChangePasswordDto changePasswordDto);
    Task<ApiResponse<bool>> UpdateUserProfileAsync(Guid userId, UpdateUserDto updateUserDto);

    // Security Features
    Task<ApiResponse<bool>> ValidateUserPermissionAsync(Guid userId, string permission);
    Task<ApiResponse<IEnumerable<string>>> GetUserRolesAsync(Guid userId);
    Task<ApiResponse<IEnumerable<string>>> GetUserPermissionsAsync(Guid userId);
    Task<ApiResponse<bool>> CheckAccountLockoutAsync(Guid employeeId);
    Task<ApiResponse<bool>> ResetAccountLockoutAsync(Guid employeeId);

    // Authentication Logs
    Task<ApiResponse<IEnumerable<AuthenticationLogDto>>> GetAuthenticationLogsAsync(Guid? employeeId = null, int pageNumber = 1, int pageSize = 20);
    Task<ApiResponse<AuthenticationStatsDto>> GetAuthenticationStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);
}

// Additional DTOs for batch operations
public class BatchImageInput
{
    public Guid EmployeeId { get; set; }
    public byte[] ImageData { get; set; } = Array.Empty<byte>();
    public string? FileName { get; set; }
}

public class BatchFaceProcessingResult
{
    public int TotalProcessed { get; set; }
    public int SuccessfullyProcessed { get; set; }
    public int Failed { get; set; }
    public List<BatchProcessingError> Errors { get; set; } = new();
    public TimeSpan ProcessingTime { get; set; }
}

public class BatchProcessingError
{
    public Guid EmployeeId { get; set; }
    public string Error { get; set; } = string.Empty;
    public string? FileName { get; set; }
}

public class AuthenticationLogDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string AuthenticationResult { get; set; } = string.Empty;
    public double? FaceMatchScore { get; set; }
    public double? LivenessScore { get; set; }
    public string? FailureReason { get; set; }
    public string? IpAddress { get; set; }
    public DateTime AttemptedAt { get; set; }
    public TimeSpan? ProcessingTime { get; set; }
}

public class AuthenticationStatsDto
{
    public int TotalAttempts { get; set; }
    public int SuccessfulAttempts { get; set; }
    public int FailedAttempts { get; set; }
    public double SuccessRate { get; set; }
    public Dictionary<string, int> FailureReasons { get; set; } = new();
    public Dictionary<string, int> AttemptsPerDay { get; set; } = new();
    public List<EmployeeAuthStatsDto> TopEmployees { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class EmployeeAuthStatsDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int TotalAttempts { get; set; }
    public int SuccessfulAttempts { get; set; }
    public double SuccessRate { get; set; }
    public DateTime LastAuthentication { get; set; }
}

public class UpdateUserDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}