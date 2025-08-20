// src/FaceGuardPro.Core/Services/RealFaceDetectionService.cs - FIXED NAMESPACE
using Microsoft.Extensions.Logging;
using AutoMapper;
using FaceGuardPro.Core.Interfaces;
using FaceGuardPro.AI.Interfaces;
using FaceGuardPro.Data.UnitOfWork;
using FaceGuardPro.Shared.Models;
using FaceGuardPro.Shared.Enums;

namespace FaceGuardPro.Core.Services;

public class RealFaceDetectionService : IFaceDetectionService
{
    private readonly IOpenCvFaceService _openCvService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<RealFaceDetectionService> _logger;

    public RealFaceDetectionService(
        IOpenCvFaceService openCvService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<RealFaceDetectionService> logger)
    {
        _openCvService = openCvService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<FaceDetectionDto>> DetectFaceAsync(byte[] imageData)
    {
        try
        {
            // Validate image
            if (!await _openCvService.ValidateImageAsync(imageData))
            {
                return ApiResponse<FaceDetectionDto>.BadRequestResult("Invalid image data");
            }

            // Detect face using OpenCV
            var openCvResult = await _openCvService.DetectFaceAsync(imageData);

            var result = new FaceDetectionDto
            {
                Result = openCvResult.Success ? FaceDetectionResult.Success : FaceDetectionResult.NoFaceDetected,
                Message = openCvResult.Message,
                Confidence = openCvResult.Confidence,
                ProcessedAt = DateTime.UtcNow
            };

            if (openCvResult.Success)
            {
                result.FaceBox = new BoundingBox
                {
                    X = openCvResult.FaceRect.X,
                    Y = openCvResult.FaceRect.Y,
                    Width = openCvResult.FaceRect.Width,
                    Height = openCvResult.FaceRect.Height
                };

                result.QualityMetrics = new FaceQualityMetrics
                {
                    OverallQuality = openCvResult.Quality.OverallScore,
                    Brightness = openCvResult.Quality.Brightness,
                    Contrast = openCvResult.Quality.Contrast,
                    Sharpness = openCvResult.Quality.Sharpness,
                    FaceSize = openCvResult.Quality.FaceSize,
                    IsBlurry = openCvResult.Quality.Issues.Contains("Image blurry"),
                    IsTooLightingPoor = openCvResult.Quality.Issues.Contains("Image too dark") ||
                                       openCvResult.Quality.Issues.Contains("Image too bright"),
                    IsFaceTooSmall = openCvResult.Quality.Issues.Contains("Face too small"),
                    IsFaceTooLarge = false // Can be enhanced later
                };

                result.Landmarks = openCvResult.Landmarks;

                // Determine specific result based on quality
                if (!openCvResult.Quality.IsAcceptable)
                {
                    if (openCvResult.Quality.Issues.Contains("Face too small"))
                        result.Result = FaceDetectionResult.TooSmall;
                    else if (openCvResult.Quality.Issues.Contains("Image blurry"))
                        result.Result = FaceDetectionResult.BlurryImage;
                    else if (openCvResult.Quality.Issues.Contains("Image too dark") ||
                             openCvResult.Quality.Issues.Contains("Image too bright"))
                        result.Result = FaceDetectionResult.BadLighting;
                    else
                        result.Result = FaceDetectionResult.PoorQuality;
                }
            }

            _logger.LogInformation("Face detection completed: Result={Result}, Confidence={Confidence}",
                result.Result, result.Confidence);

            return ApiResponse<FaceDetectionDto>.SuccessResult(result, "Face detection completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in face detection");
            return ApiResponse<FaceDetectionDto>.ErrorResult("Face detection error occurred");
        }
    }

    public async Task<ApiResponse<IEnumerable<FaceDetectionDto>>> DetectMultipleFacesAsync(byte[] imageData)
    {
        try
        {
            if (!await _openCvService.ValidateImageAsync(imageData))
            {
                return ApiResponse<IEnumerable<FaceDetectionDto>>.BadRequestResult("Invalid image data");
            }

            var openCvResults = await _openCvService.DetectMultipleFacesAsync(imageData);
            var results = new List<FaceDetectionDto>();

            foreach (var openCvResult in openCvResults)
            {
                var result = new FaceDetectionDto
                {
                    Result = openCvResult.Success ? FaceDetectionResult.Success : FaceDetectionResult.NoFaceDetected,
                    Message = openCvResult.Message,
                    Confidence = openCvResult.Confidence,
                    ProcessedAt = DateTime.UtcNow
                };

                if (openCvResult.Success)
                {
                    result.FaceBox = new BoundingBox
                    {
                        X = openCvResult.FaceRect.X,
                        Y = openCvResult.FaceRect.Y,
                        Width = openCvResult.FaceRect.Width,
                        Height = openCvResult.FaceRect.Height
                    };

                    result.QualityMetrics = new FaceQualityMetrics
                    {
                        OverallQuality = openCvResult.Quality.OverallScore,
                        Brightness = openCvResult.Quality.Brightness,
                        Contrast = openCvResult.Quality.Contrast,
                        Sharpness = openCvResult.Quality.Sharpness,
                        FaceSize = openCvResult.Quality.FaceSize
                    };

                    result.Landmarks = openCvResult.Landmarks;
                }

                results.Add(result);
            }

            return ApiResponse<IEnumerable<FaceDetectionDto>>.SuccessResult(results,
                $"Detected {results.Count} faces");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in multiple face detection");
            return ApiResponse<IEnumerable<FaceDetectionDto>>.ErrorResult("Multiple face detection error occurred");
        }
    }

    public async Task<ApiResponse<FaceQualityMetrics>> AnalyzeFaceQualityAsync(byte[] imageData)
    {
        try
        {
            var qualityResult = await _openCvService.AnalyzeFaceQualityAsync(imageData);

            var metrics = new FaceQualityMetrics
            {
                OverallQuality = qualityResult.OverallScore,
                Brightness = qualityResult.Brightness,
                Contrast = qualityResult.Contrast,
                Sharpness = qualityResult.Sharpness,
                FaceSize = qualityResult.FaceSize,
                IsBlurry = qualityResult.Issues.Contains("Image blurry"),
                IsTooLightingPoor = qualityResult.Issues.Contains("Image too dark") ||
                                   qualityResult.Issues.Contains("Image too bright"),
                IsFaceTooSmall = qualityResult.Issues.Contains("Face too small"),
                IsFaceTooLarge = false
            };

            return ApiResponse<FaceQualityMetrics>.SuccessResult(metrics, "Quality analysis completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in face quality analysis");
            return ApiResponse<FaceQualityMetrics>.ErrorResult("Quality analysis error occurred");
        }
    }

    public async Task<ApiResponse<FaceTemplateDto>> CreateFaceTemplateAsync(CreateFaceTemplateDto createTemplateDto)
    {
        try
        {
            // Detect face first
            var detection = await DetectFaceAsync(createTemplateDto.ImageData);
            if (!detection.Success || detection.Data?.Result != FaceDetectionResult.Success)
            {
                return ApiResponse<FaceTemplateDto>.BadRequestResult("Face detection failed");
            }

            // Check if employee exists
            var employee = await _unitOfWork.Employees.GetByIdAsync(createTemplateDto.EmployeeId);
            if (employee == null)
            {
                return ApiResponse<FaceTemplateDto>.NotFoundResult("Employee not found");
            }

            // Create face template using OpenCV
            var aiTemplate = await _openCvService.CreateFaceTemplateAsync(createTemplateDto.ImageData);

            // Save to database
            var faceTemplate = new Data.Entities.FaceTemplate
            {
                EmployeeId = createTemplateDto.EmployeeId,
                TemplateData = aiTemplate.TemplateData,
                Quality = aiTemplate.Quality,
                Metadata = System.Text.Json.JsonSerializer.Serialize(aiTemplate.Metadata),
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.FaceTemplates.AddAsync(faceTemplate);
            await _unitOfWork.SaveChangesAsync();

            var result = new FaceTemplateDto
            {
                Id = faceTemplate.Id,
                EmployeeId = faceTemplate.EmployeeId,
                TemplateData = faceTemplate.TemplateData,
                Quality = faceTemplate.Quality,
                CreatedAt = faceTemplate.CreatedAt,
                EmployeeName = employee.FullName,
                EmployeeId_Display = employee.EmployeeId
            };

            _logger.LogInformation("Face template created for employee {EmployeeId}", employee.EmployeeId);
            return ApiResponse<FaceTemplateDto>.SuccessResult(result, "Face template created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating face template for employee {EmployeeId}",
                createTemplateDto.EmployeeId);
            return ApiResponse<FaceTemplateDto>.ErrorResult("Face template creation error occurred");
        }
    }

    public async Task<ApiResponse<FaceTemplateDto>> GetFaceTemplateAsync(Guid employeeId)
    {
        try
        {
            var faceTemplate = await _unitOfWork.FaceTemplates.GetByEmployeeIdAsync(employeeId);
            if (faceTemplate == null)
            {
                return ApiResponse<FaceTemplateDto>.NotFoundResult("Face template not found");
            }

            var result = _mapper.Map<FaceTemplateDto>(faceTemplate);
            return ApiResponse<FaceTemplateDto>.SuccessResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving face template for employee {EmployeeId}", employeeId);
            return ApiResponse<FaceTemplateDto>.ErrorResult("Error retrieving face template");
        }
    }

    public async Task<ApiResponse<bool>> UpdateFaceTemplateAsync(Guid employeeId, byte[] newImageData)
    {
        try
        {
            // First delete existing template
            await _unitOfWork.FaceTemplates.DeleteByEmployeeIdAsync(employeeId);

            // Create new template
            var createDto = new CreateFaceTemplateDto
            {
                EmployeeId = employeeId,
                ImageData = newImageData
            };

            var result = await CreateFaceTemplateAsync(createDto);
            return ApiResponse<bool>.SuccessResult(result.Success,
                result.Success ? "Face template updated successfully" : result.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating face template for employee {EmployeeId}", employeeId);
            return ApiResponse<bool>.ErrorResult("Face template update error occurred");
        }
    }

    public async Task<ApiResponse<bool>> DeleteFaceTemplateAsync(Guid employeeId)
    {
        try
        {
            var deletedCount = await _unitOfWork.FaceTemplates.DeleteByEmployeeIdAsync(employeeId);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResult(deletedCount > 0,
                deletedCount > 0 ? "Face template deleted successfully" : "No face template found to delete");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting face template for employee {EmployeeId}", employeeId);
            return ApiResponse<bool>.ErrorResult("Face template deletion error occurred");
        }
    }

    public async Task<ApiResponse<FaceComparisonResult>> CompareFacesAsync(byte[] image1Data, byte[] image2Data)
    {
        try
        {
            var openCvResult = await _openCvService.CompareFacesAsync(image1Data, image2Data);

            var result = new FaceComparisonResult
            {
                IsMatch = openCvResult.IsMatch,
                Similarity = openCvResult.Similarity,
                Threshold = openCvResult.Threshold,
                Message = openCvResult.IsMatch ? "Faces match" : "Faces do not match",
                ComparedAt = DateTime.UtcNow
            };

            return ApiResponse<FaceComparisonResult>.SuccessResult(result, "Face comparison completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing faces");
            return ApiResponse<FaceComparisonResult>.ErrorResult("Face comparison error occurred");
        }
    }

    public async Task<ApiResponse<FaceComparisonResult>> CompareWithTemplateAsync(Guid employeeId, byte[] imageData)
    {
        try
        {
            // Get stored template
            var templateResult = await GetFaceTemplateAsync(employeeId);
            if (!templateResult.Success || templateResult.Data == null)
            {
                return ApiResponse<FaceComparisonResult>.NotFoundResult("Face template not found");
            }

            // Convert database template to AI template
            var aiTemplate = new AI.Interfaces.OpenCvFaceTemplate
            {
                EmployeeId = employeeId,
                TemplateData = templateResult.Data.TemplateData,
                Quality = templateResult.Data.Quality
            };

            var openCvResult = await _openCvService.CompareWithTemplateAsync(aiTemplate, imageData);

            var result = new FaceComparisonResult
            {
                IsMatch = openCvResult.IsMatch,
                Similarity = openCvResult.Similarity,
                Threshold = openCvResult.Threshold,
                Message = openCvResult.IsMatch ? "Face matches template" : "Face does not match template",
                ComparedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Face compared with template for employee {EmployeeId}: Match={IsMatch}, Similarity={Similarity}",
                employeeId, result.IsMatch, result.Similarity);

            return ApiResponse<FaceComparisonResult>.SuccessResult(result, "Template comparison completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing with template for employee {EmployeeId}", employeeId);
            return ApiResponse<FaceComparisonResult>.ErrorResult("Template comparison error occurred");
        }
    }

    // Stub methods for liveness detection (Stage 4 da implement qilinadi)
    public async Task<ApiResponse<LivenessDetectionDto>> DetectLivenessAsync(byte[] imageData)
    {
        var result = new LivenessDetectionDto
        {
            Result = LivenessResult.Live,
            Message = "Liveness detection not yet implemented - returning default result",
            Confidence = 0.90,
            ProcessedAt = DateTime.UtcNow
        };

        return ApiResponse<LivenessDetectionDto>.SuccessResult(result);
    }

    public async Task<ApiResponse<LivenessDetectionDto>> DetectLivenessFromStreamAsync(IEnumerable<byte[]> frameData)
    {
        return ApiResponse<LivenessDetectionDto>.ErrorResult("Liveness from stream not yet implemented");
    }

    // Advanced features (Stage 6 da implement qilinadi)
    public async Task<ApiResponse<EmployeeDto>> RecognizeFaceAsync(byte[] imageData)
    {
        return ApiResponse<EmployeeDto>.ErrorResult("Face recognition search not yet implemented");
    }

    public async Task<ApiResponse<IEnumerable<EmployeeDto>>> RecognizeMultipleFacesAsync(byte[] imageData)
    {
        return ApiResponse<IEnumerable<EmployeeDto>>.ErrorResult("Multiple face recognition not yet implemented");
    }

    public async Task<ApiResponse<IEnumerable<FaceComparisonResult>>> FindSimilarFacesAsync(byte[] imageData, double threshold = 0.8)
    {
        return ApiResponse<IEnumerable<FaceComparisonResult>>.ErrorResult("Similar face search not yet implemented");
    }

    public async Task<ApiResponse<IEnumerable<FaceTemplateDto>>> GetAllFaceTemplatesAsync()
    {
        try
        {
            var templates = await _unitOfWork.FaceTemplates.GetAllAsync();
            var result = _mapper.Map<IEnumerable<FaceTemplateDto>>(templates);
            return ApiResponse<IEnumerable<FaceTemplateDto>>.SuccessResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all face templates");
            return ApiResponse<IEnumerable<FaceTemplateDto>>.ErrorResult("Error retrieving face templates");
        }
    }

    public async Task<ApiResponse<IEnumerable<FaceTemplateDto>>> GetHighQualityTemplatesAsync(double minQuality = 0.8)
    {
        try
        {
            var templates = await _unitOfWork.FaceTemplates.GetHighQualityTemplatesAsync(minQuality);
            var result = _mapper.Map<IEnumerable<FaceTemplateDto>>(templates);
            return ApiResponse<IEnumerable<FaceTemplateDto>>.SuccessResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving high quality templates");
            return ApiResponse<IEnumerable<FaceTemplateDto>>.ErrorResult("Error retrieving high quality templates");
        }
    }

    public async Task<ApiResponse<bool>> ValidateTemplateQualityAsync(byte[] templateData)
    {
        return ApiResponse<bool>.ErrorResult("Template quality validation not yet implemented");
    }

    public async Task<ApiResponse<BatchFaceProcessingResult>> ProcessMultipleImagesAsync(IEnumerable<BatchImageInput> images)
    {
        return ApiResponse<BatchFaceProcessingResult>.ErrorResult("Batch processing not yet implemented");
    }

    public async Task<ApiResponse<int>> RegenerateLowQualityTemplatesAsync(double qualityThreshold = 0.6)
    {
        return ApiResponse<int>.ErrorResult("Template regeneration not yet implemented");
    }
}