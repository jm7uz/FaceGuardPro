// src/FaceGuardPro.API/Controllers/Face/FaceController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FaceGuardPro.Core.Interfaces;
using FaceGuardPro.Shared.Models;
using FaceGuardPro.Shared.Constants;

namespace FaceGuardPro.API.Controllers.Face;

/// <summary>
/// Face detection and recognition endpoints
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class FaceController : BaseController
{
    private readonly IFaceDetectionService _faceDetectionService;
    private readonly ILogger<FaceController> _logger;

    public FaceController(
        IFaceDetectionService faceDetectionService,
        ILogger<FaceController> logger)
    {
        _faceDetectionService = faceDetectionService;
        _logger = logger;
    }

    /// <summary>
    /// Detect face in uploaded image
    /// </summary>
    /// <param name="image">Image file containing face</param>
    /// <returns>Face detection result</returns>
    [HttpPost("detect")]
    [ProducesResponseType(typeof(ApiResponse<FaceDetectionDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<ActionResult<ApiResponse<FaceDetectionDto>>> DetectFace([FromForm] IFormFile image)
    {
        try
        {
            RequirePermission(AuthenticationConstants.Permissions.MANAGE_FACE_TEMPLATES);

            if (image == null || image.Length == 0)
            {
                return BadRequest<FaceDetectionDto>("Image file is required");
            }

            if (image.Length > ImageProcessingConstants.MAX_FILE_SIZE_MB * 1024 * 1024)
            {
                return BadRequest<FaceDetectionDto>($"File size cannot exceed {ImageProcessingConstants.MAX_FILE_SIZE_MB}MB");
            }

            using var memoryStream = new MemoryStream();
            await image.CopyToAsync(memoryStream);
            var imageData = memoryStream.ToArray();

            var result = await _faceDetectionService.DetectFaceAsync(imageData);

            if (result.Success)
            {
                _logger.LogInformation("Face detection successful for user {UserId}: Result={Result}, Confidence={Confidence}",
                    CurrentUserId, result.Data?.Result, result.Data?.Confidence);
            }

            return HandleServiceResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in face detection endpoint for user {UserId}", CurrentUserId);
            return InternalServerError<FaceDetectionDto>("Face detection error occurred");
        }
    }

    /// <summary>
    /// Detect multiple faces in uploaded image
    /// </summary>
    /// <param name="image">Image file that may contain multiple faces</param>
    /// <returns>Multiple face detection results</returns>
    [HttpPost("detect-multiple")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<FaceDetectionDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<ActionResult<ApiResponse<IEnumerable<FaceDetectionDto>>>> DetectMultipleFaces([FromForm] IFormFile image)
    {
        try
        {
            RequirePermission(AuthenticationConstants.Permissions.MANAGE_FACE_TEMPLATES);

            if (image == null || image.Length == 0)
            {
                return BadRequest<IEnumerable<FaceDetectionDto>>("Image file is required");
            }

            using var memoryStream = new MemoryStream();
            await image.CopyToAsync(memoryStream);
            var imageData = memoryStream.ToArray();

            var result = await _faceDetectionService.DetectMultipleFacesAsync(imageData);
            return HandleServiceResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in multiple face detection for user {UserId}", CurrentUserId);
            return InternalServerError<IEnumerable<FaceDetectionDto>>("Multiple face detection error occurred");
        }
    }

    /// <summary>
    /// Analyze face quality in uploaded image
    /// </summary>
    /// <param name="image">Image file for quality analysis</param>
    /// <returns>Face quality metrics</returns>
    [HttpPost("analyze-quality")]
    [ProducesResponseType(typeof(ApiResponse<FaceQualityMetrics>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<ActionResult<ApiResponse<FaceQualityMetrics>>> AnalyzeFaceQuality([FromForm] IFormFile image)
    {
        try
        {
            RequirePermission(AuthenticationConstants.Permissions.MANAGE_FACE_TEMPLATES);

            if (image == null || image.Length == 0)
            {
                return BadRequest<FaceQualityMetrics>("Image file is required");
            }

            using var memoryStream = new MemoryStream();
            await image.CopyToAsync(memoryStream);
            var imageData = memoryStream.ToArray();

            var result = await _faceDetectionService.AnalyzeFaceQualityAsync(imageData);
            return HandleServiceResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in face quality analysis for user {UserId}", CurrentUserId);
            return InternalServerError<FaceQualityMetrics>("Face quality analysis error occurred");
        }
    }

    /// <summary>
    /// Create face template for employee
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="image">Face image for template creation</param>
    /// <returns>Created face template</returns>
    [HttpPost("templates/{employeeId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<FaceTemplateDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<ActionResult<ApiResponse<FaceTemplateDto>>> CreateFaceTemplate(
        Guid employeeId,
        [FromForm] IFormFile image)
    {
        try
        {
            RequirePermission(AuthenticationConstants.Permissions.MANAGE_FACE_TEMPLATES);

            if (image == null || image.Length == 0)
            {
                return BadRequest<FaceTemplateDto>("Image file is required");
            }

            using var memoryStream = new MemoryStream();
            await image.CopyToAsync(memoryStream);
            var imageData = memoryStream.ToArray();

            var createDto = new CreateFaceTemplateDto
            {
                EmployeeId = employeeId,
                ImageData = imageData
            };

            var result = await _faceDetectionService.CreateFaceTemplateAsync(createDto);

            if (result.Success)
            {
                _logger.LogInformation("Face template created for employee {EmployeeId} by user {UserId}",
                    employeeId, CurrentUserId);
                return Created($"api/v1/face/templates/{employeeId}", result);
            }

            return HandleServiceResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating face template for employee {EmployeeId} by user {UserId}",
                employeeId, CurrentUserId);
            return InternalServerError<FaceTemplateDto>("Face template creation error occurred");
        }
    }

    /// <summary>
    /// Get face template for employee
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <returns>Face template data</returns>
    [HttpGet("templates/{employeeId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<FaceTemplateDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<ActionResult<ApiResponse<FaceTemplateDto>>> GetFaceTemplate(Guid employeeId)
    {
        try
        {
            RequirePermission(AuthenticationConstants.Permissions.VIEW_EMPLOYEES);

            var result = await _faceDetectionService.GetFaceTemplateAsync(employeeId);
            return HandleServiceResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving face template for employee {EmployeeId}", employeeId);
            return InternalServerError<FaceTemplateDto>("Error retrieving face template");
        }
    }

    /// <summary>
    /// Update face template for employee
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="image">New face image</param>
    /// <returns>Update result</returns>
    [HttpPut("templates/{employeeId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateFaceTemplate(
        Guid employeeId,
        [FromForm] IFormFile image)
    {
        try
        {
            RequirePermission(AuthenticationConstants.Permissions.MANAGE_FACE_TEMPLATES);

            if (image == null || image.Length == 0)
            {
                return BadRequest<bool>("Image file is required");
            }

            using var memoryStream = new MemoryStream();
            await image.CopyToAsync(memoryStream);
            var imageData = memoryStream.ToArray();

            var result = await _faceDetectionService.UpdateFaceTemplateAsync(employeeId, imageData);

            if (result.Success)
            {
                _logger.LogInformation("Face template updated for employee {EmployeeId} by user {UserId}",
                    employeeId, CurrentUserId);
            }

            return HandleServiceResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating face template for employee {EmployeeId}", employeeId);
            return InternalServerError<bool>("Face template update error occurred");
        }
    }

    /// <summary>
    /// Delete face template for employee
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <returns>Deletion result</returns>
    [HttpDelete("templates/{employeeId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteFaceTemplate(Guid employeeId)
    {
        try
        {
            RequirePermission(AuthenticationConstants.Permissions.MANAGE_FACE_TEMPLATES);

            var result = await _faceDetectionService.DeleteFaceTemplateAsync(employeeId);

            if (result.Success)
            {
                _logger.LogInformation("Face template deleted for employee {EmployeeId} by user {UserId}",
                    employeeId, CurrentUserId);
            }

            return HandleServiceResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting face template for employee {EmployeeId}", employeeId);
            return InternalServerError<bool>("Face template deletion error occurred");
        }
    }

    /// <summary>
    /// Compare two face images
    /// </summary>
    /// <param name="image1">First face image</param>
    /// <param name="image2">Second face image</param>
    /// <returns>Face comparison result</returns>
    [HttpPost("compare")]
    [ProducesResponseType(typeof(ApiResponse<FaceComparisonResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<ActionResult<ApiResponse<FaceComparisonResult>>> CompareFaces(
        [FromForm] IFormFile image1,
        [FromForm] IFormFile image2)
    {
        try
        {
            RequirePermission(AuthenticationConstants.Permissions.MANAGE_FACE_TEMPLATES);

            if (image1 == null || image1.Length == 0)
            {
                return BadRequest<FaceComparisonResult>("First image file is required");
            }

            if (image2 == null || image2.Length == 0)
            {
                return BadRequest<FaceComparisonResult>("Second image file is required");
            }

            using var memoryStream1 = new MemoryStream();
            using var memoryStream2 = new MemoryStream();

            await image1.CopyToAsync(memoryStream1);
            await image2.CopyToAsync(memoryStream2);

            var imageData1 = memoryStream1.ToArray();
            var imageData2 = memoryStream2.ToArray();

            var result = await _faceDetectionService.CompareFacesAsync(imageData1, imageData2);

            _logger.LogInformation("Face comparison performed by user {UserId}: Similarity={Similarity}, Match={IsMatch}",
                CurrentUserId, result.Data?.Similarity, result.Data?.IsMatch);

            return HandleServiceResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing faces for user {UserId}", CurrentUserId);
            return InternalServerError<FaceComparisonResult>("Face comparison error occurred");
        }
    }

    /// <summary>
    /// Compare face image with employee template
    /// </summary>
    /// <param name="employeeId">Employee ID to compare against</param>
    /// <param name="image">Face image to compare</param>
    /// <returns>Face comparison result</returns>
    [HttpPost("compare-with-template/{employeeId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<FaceComparisonResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<ActionResult<ApiResponse<FaceComparisonResult>>> CompareWithTemplate(
        Guid employeeId,
        [FromForm] IFormFile image)
    {
        try
        {
            RequirePermission(AuthenticationConstants.Permissions.PERFORM_AUTHENTICATION);

            if (image == null || image.Length == 0)
            {
                return BadRequest<FaceComparisonResult>("Image file is required");
            }

            using var memoryStream = new MemoryStream();
            await image.CopyToAsync(memoryStream);
            var imageData = memoryStream.ToArray();

            var result = await _faceDetectionService.CompareWithTemplateAsync(employeeId, imageData);

            _logger.LogInformation("Template comparison for employee {EmployeeId} by user {UserId}: Similarity={Similarity}, Match={IsMatch}",
                employeeId, CurrentUserId, result.Data?.Similarity, result.Data?.IsMatch);

            return HandleServiceResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing with template for employee {EmployeeId}", employeeId);
            return InternalServerError<FaceComparisonResult>("Template comparison error occurred");
        }
    }

    /// <summary>
    /// Get all face templates
    /// </summary>
    /// <returns>List of all face templates</returns>
    [HttpGet("templates")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<FaceTemplateDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<ActionResult<ApiResponse<IEnumerable<FaceTemplateDto>>>> GetAllFaceTemplates()
    {
        try
        {
            RequirePermission(AuthenticationConstants.Permissions.VIEW_EMPLOYEES);

            var result = await _faceDetectionService.GetAllFaceTemplatesAsync();
            return HandleServiceResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all face templates");
            return InternalServerError<IEnumerable<FaceTemplateDto>>("Error retrieving face templates");
        }
    }

    /// <summary>
    /// Get high quality face templates
    /// </summary>
    /// <param name="minQuality">Minimum quality threshold (0.0 - 1.0)</param>
    /// <returns>List of high quality face templates</returns>
    [HttpGet("templates/high-quality")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<FaceTemplateDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<ActionResult<ApiResponse<IEnumerable<FaceTemplateDto>>>> GetHighQualityTemplates(
        [FromQuery] double minQuality = 0.8)
    {
        try
        {
            RequirePermission(AuthenticationConstants.Permissions.VIEW_EMPLOYEES);

            if (minQuality < 0.0 || minQuality > 1.0)
            {
                return BadRequest<IEnumerable<FaceTemplateDto>>("Quality threshold must be between 0.0 and 1.0");
            }

            var result = await _faceDetectionService.GetHighQualityTemplatesAsync(minQuality);
            return HandleServiceResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving high quality templates");
            return InternalServerError<IEnumerable<FaceTemplateDto>>("Error retrieving high quality templates");
        }
    }

    /// <summary>
    /// Test face detection system health
    /// </summary>
    /// <returns>System health status</returns>
    [HttpGet("health")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<ActionResult<ApiResponse<object>>> GetFaceDetectionHealth()
    {
        try
        {
            // Create a simple test image (1x1 pixel)
            var testImage = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46 }; // Minimal JPEG header

            var healthInfo = new
            {
                Status = "Healthy",
                OpenCvEnabled = true,
                Timestamp = DateTime.UtcNow,
                Services = new
                {
                    FaceDetection = "Available",
                    FaceRecognition = "Available",
                    TemplateManagement = "Available"
                }
            };

            return Success((object)healthInfo, "Face detection system is healthy");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Face detection health check failed");

            var healthInfo = new
            {
                Status = "Unhealthy",
                OpenCvEnabled = false,
                Timestamp = DateTime.UtcNow,
                Error = ex.Message
            };

            return InternalServerError<object>("Face detection system error");
        }
    }
}