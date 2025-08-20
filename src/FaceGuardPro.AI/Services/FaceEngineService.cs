using FaceGuardPro.AI.Configuration;
using FaceGuardPro.AI.DTOs;
using FaceGuardPro.AI.Interfaces;
using FaceGuardPro.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Drawing;

namespace FaceGuardPro.AI.Services
{
    /// <summary>
    /// Real Face Engine Service - replaces stub implementation
    /// Integrates face detection and recognition services
    /// </summary>
    public class FaceEngineService : IFaceEngineService
    {
        private readonly ILogger<FaceEngineService> _logger;
        private readonly IFaceDetectionService _faceDetectionService;
        private readonly IFaceRecognitionService _faceRecognitionService;
        private readonly FaceDetectionConfig _config;

        public FaceEngineService(
            ILogger<FaceEngineService> logger,
            IFaceDetectionService faceDetectionService,
            IFaceRecognitionService faceRecognitionService,
            FaceDetectionConfig? config = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _faceDetectionService = faceDetectionService ?? throw new ArgumentNullException(nameof(faceDetectionService));
            _faceRecognitionService = faceRecognitionService ?? throw new ArgumentNullException(nameof(faceRecognitionService));
            _config = config ?? FaceDetectionConfig.Default;

            _logger.LogInformation("FaceEngineService initialized with real OpenCV implementation");
        }

        public async Task<FaceDetectionResultDto> DetectFaceAsync(
            byte[] imageData,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Starting face detection for image ({Size} bytes)", imageData?.Length ?? 0);

                if (imageData == null || imageData.Length == 0)
                {
                    return new FaceDetectionResultDto
                    {
                        Success = false,
                        Message = "Image data is null or empty",
                        ProcessingTimeMs = 0
                    };
                }

                var options = new FaceProcessingOptions
                {
                    DetectMultipleFaces = false, // Single face detection for authentication
                    GenerateTemplate = false,
                    CalculateQuality = true,
                    ResizeImage = true,
                    ApplyHistogramEqualization = true
                };

                var detectionResult = await _faceDetectionService.DetectFacesAsync(
                    imageData, options, cancellationToken);

                var result = new FaceDetectionResultDto
                {
                    Success = detectionResult.Success,
                    Message = detectionResult.Success ? "Face detected successfully" :
                             (detectionResult.ErrorMessage ?? "No face detected"),
                    ProcessingTimeMs = detectionResult.ProcessingTimeMs,
                    FaceCount = detectionResult.FaceCount
                };

                if (detectionResult.Success && detectionResult.Faces.Count > 0)
                {
                    var bestFace = detectionResult.Faces.First(); // Already sorted by quality
                    result.FaceRegion = new FaceRegionDto
                    {
                        X = bestFace.BoundingBox.X,
                        Y = bestFace.BoundingBox.Y,
                        Width = bestFace.BoundingBox.Width,
                        Height = bestFace.BoundingBox.Height
                    };
                    result.QualityScore = bestFace.QualityScore;
                    result.Confidence = bestFace.Confidence;
                }

                _logger.LogDebug("Face detection completed: success={Success}, faces={FaceCount}, time={TimeMs}ms",
                    result.Success, result.FaceCount, result.ProcessingTimeMs);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during face detection");
                return new FaceDetectionResultDto
                {
                    Success = false,
                    Message = $"Face detection failed: {ex.Message}",
                    ProcessingTimeMs = 0
                };
            }
        }

        public async Task<FaceTemplateResultDto> GenerateFaceTemplateAsync(
            byte[] imageData,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Starting face template generation for image ({Size} bytes)", imageData?.Length ?? 0);

                if (imageData == null || imageData.Length == 0)
                {
                    return new FaceTemplateResultDto
                    {
                        Success = false,
                        Message = "Image data is null or empty",
                        ProcessingTimeMs = 0
                    };
                }

                // First detect face
                var detectionResult = await DetectFaceAsync(imageData, cancellationToken);

                if (!detectionResult.Success || detectionResult.FaceRegion == null)
                {
                    return new FaceTemplateResultDto
                    {
                        Success = false,
                        Message = $"Face detection failed: {detectionResult.Message}",
                        ProcessingTimeMs = detectionResult.ProcessingTimeMs
                    };
                }

                // Check quality threshold
                if (detectionResult.QualityScore < _config.QualityThreshold)
                {
                    return new FaceTemplateResultDto
                    {
                        Success = false,
                        Message = $"Face quality too low: {detectionResult.QualityScore:F1}% (minimum: {_config.QualityThreshold}%)",
                        ProcessingTimeMs = detectionResult.ProcessingTimeMs,
                        QualityScore = detectionResult.QualityScore
                    };
                }

                // Generate template
                var faceRegion = new Rectangle(
                    detectionResult.FaceRegion.X,
                    detectionResult.FaceRegion.Y,
                    detectionResult.FaceRegion.Width,
                    detectionResult.FaceRegion.Height);

                var templateResult = await _faceRecognitionService.GenerateTemplateAsync(
                    imageData, faceRegion, cancellationToken);

                var result = new FaceTemplateResultDto
                {
                    Success = templateResult.Success,
                    Message = templateResult.Success ? "Template generated successfully" :
                             (templateResult.ErrorMessage ?? "Template generation failed"),
                    ProcessingTimeMs = detectionResult.ProcessingTimeMs + templateResult.ProcessingTimeMs,
                    TemplateData = templateResult.TemplateData,
                    QualityScore = detectionResult.QualityScore,
                    TemplateSize = templateResult.TemplateSize,
                    Version = templateResult.Version
                };

                _logger.LogDebug("Template generation completed: success={Success}, size={Size} bytes, quality={Quality}%",
                    result.Success, result.TemplateSize, result.QualityScore);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during face template generation");
                return new FaceTemplateResultDto
                {
                    Success = false,
                    Message = $"Template generation failed: {ex.Message}",
                    ProcessingTimeMs = 0
                };
            }
        }

        public async Task<FaceComparisonResultDto> CompareFacesAsync(
            byte[] imageData1,
            byte[] imageData2,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Starting face comparison between two images");

                if (imageData1 == null || imageData1.Length == 0 ||
                    imageData2 == null || imageData2.Length == 0)
                {
                    return new FaceComparisonResultDto
                    {
                        Success = false,
                        Message = "One or both image data is null or empty",
                        ProcessingTimeMs = 0
                    };
                }

                // Generate templates for both images
                var template1Task = GenerateFaceTemplateAsync(imageData1, cancellationToken);
                var template2Task = GenerateFaceTemplateAsync(imageData2, cancellationToken);

                var template1Result = await template1Task;
                var template2Result = await template2Task;

                if (!template1Result.Success)
                {
                    return new FaceComparisonResultDto
                    {
                        Success = false,
                        Message = $"Failed to generate template from first image: {template1Result.Message}",
                        ProcessingTimeMs = template1Result.ProcessingTimeMs
                    };
                }

                if (!template2Result.Success)
                {
                    return new FaceComparisonResultDto
                    {
                        Success = false,
                        Message = $"Failed to generate template from second image: {template2Result.Message}",
                        ProcessingTimeMs = template1Result.ProcessingTimeMs + template2Result.ProcessingTimeMs
                    };
                }

                // Compare templates
                var comparisonResult = await _faceRecognitionService.CompareTemplatesAsync(
                    template1Result.TemplateData!, template2Result.TemplateData!, cancellationToken);

                var result = new FaceComparisonResultDto
                {
                    Success = comparisonResult.Success,
                    Message = comparisonResult.Success ? "Face comparison completed" :
                             (comparisonResult.ErrorMessage ?? "Comparison failed"),
                    ProcessingTimeMs = template1Result.ProcessingTimeMs + template2Result.ProcessingTimeMs + comparisonResult.ProcessingTimeMs,
                    SimilarityScore = comparisonResult.SimilarityScore,
                    IsMatch = comparisonResult.IsMatch,
                    Confidence = comparisonResult.Confidence,
                    Distance = comparisonResult.Distance,
                    Threshold = _config.RecognitionThreshold
                };

                _logger.LogDebug("Face comparison completed: similarity={Similarity}%, match={IsMatch}, confidence={Confidence}%",
                    result.SimilarityScore, result.IsMatch, result.Confidence);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during face comparison");
                return new FaceComparisonResultDto
                {
                    Success = false,
                    Message = $"Face comparison failed: {ex.Message}",
                    ProcessingTimeMs = 0
                };
            }
        }

        public async Task<FaceComparisonResultDto> CompareWithStoredTemplateAsync(
            byte[] imageData,
            byte[] storedTemplate,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Starting face comparison with stored template");

                if (imageData == null || imageData.Length == 0)
                {
                    return new FaceComparisonResultDto
                    {
                        Success = false,
                        Message = "Image data is null or empty",
                        ProcessingTimeMs = 0
                    };
                }

                if (storedTemplate == null || storedTemplate.Length == 0)
                {
                    return new FaceComparisonResultDto
                    {
                        Success = false,
                        Message = "Stored template is null or empty",
                        ProcessingTimeMs = 0
                    };
                }

                // Validate stored template
                if (!_faceRecognitionService.IsValidTemplate(storedTemplate))
                {
                    return new FaceComparisonResultDto
                    {
                        Success = false,
                        Message = "Invalid stored template format",
                        ProcessingTimeMs = 0
                    };
                }

                // Generate template from input image
                var templateResult = await GenerateFaceTemplateAsync(imageData, cancellationToken);

                if (!templateResult.Success || templateResult.TemplateData == null)
                {
                    return new FaceComparisonResultDto
                    {
                        Success = false,
                        Message = $"Failed to generate template from image: {templateResult.Message}",
                        ProcessingTimeMs = templateResult.ProcessingTimeMs
                    };
                }

                // Compare templates
                var comparisonResult = await _faceRecognitionService.CompareTemplatesAsync(
                    templateResult.TemplateData, storedTemplate, cancellationToken);

                var result = new FaceComparisonResultDto
                {
                    Success = comparisonResult.Success,
                    Message = comparisonResult.Success ? "Template comparison completed" :
                             (comparisonResult.ErrorMessage ?? "Comparison failed"),
                    ProcessingTimeMs = templateResult.ProcessingTimeMs + comparisonResult.ProcessingTimeMs,
                    SimilarityScore = comparisonResult.SimilarityScore,
                    IsMatch = comparisonResult.IsMatch,
                    Confidence = comparisonResult.Confidence,
                    Distance = comparisonResult.Distance,
                    Threshold = _config.RecognitionThreshold
                };

                _logger.LogDebug("Template comparison completed: similarity={Similarity}%, match={IsMatch}",
                    result.SimilarityScore, result.IsMatch);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during template comparison");
                return new FaceComparisonResultDto
                {
                    Success = false,
                    Message = $"Template comparison failed: {ex.Message}",
                    ProcessingTimeMs = 0
                };
            }
        }

        public async Task<LivenessDetectionResultDto> DetectLivenessAsync(
            byte[] imageData,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Starting liveness detection (basic implementation)");

                // Basic liveness detection using face quality assessment
                var detectionResult = await DetectFaceAsync(imageData, cancellationToken);

                if (!detectionResult.Success)
                {
                    return new LivenessDetectionResultDto
                    {
                        Success = false,
                        Message = $"Face detection failed: {detectionResult.Message}",
                        ProcessingTimeMs = detectionResult.ProcessingTimeMs,
                        IsLive = false,
                        Confidence = 0
                    };
                }

                // Basic liveness assessment based on quality metrics
                var isLive = detectionResult.QualityScore >= _config.QualityThreshold &&
                           detectionResult.Confidence >= 80.0;

                var confidence = Math.Min(100.0, (detectionResult.QualityScore + detectionResult.Confidence) / 2.0);

                var result = new LivenessDetectionResultDto
                {
                    Success = true,
                    Message = isLive ? "Live face detected" : "Potential spoof detected",
                    ProcessingTimeMs = detectionResult.ProcessingTimeMs,
                    IsLive = isLive,
                    Confidence = confidence,
                    QualityScore = detectionResult.QualityScore
                };

                _logger.LogDebug("Liveness detection completed: isLive={IsLive}, confidence={Confidence}%",
                    result.IsLive, result.Confidence);

                return result;

                // TODO: Stage 4 da advanced liveness detection implement qilamiz:
                // - Eye blink detection
                // - Head movement detection  
                // - Texture analysis
                // - Challenge-response system
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during liveness detection");
                return new LivenessDetectionResultDto
                {
                    Success = false,
                    Message = $"Liveness detection failed: {ex.Message}",
                    ProcessingTimeMs = 0,
                    IsLive = false,
                    Confidence = 0
                };
            }
        }

        public async Task<bool> ValidateTemplateAsync(
            byte[] templateData,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (templateData == null || templateData.Length == 0)
                {
                    return false;
                }

                return await Task.FromResult(_faceRecognitionService.IsValidTemplate(templateData));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating template");
                return false;
            }
        }

        public async Task<Dictionary<string, object>> GetEngineStatisticsAsync()
        {
            try
            {
                var detectionStats = await _faceDetectionService.GetStatisticsAsync();
                var recognitionStats = await _faceRecognitionService.GetStatisticsAsync();

                var combinedStats = new Dictionary<string, object>
                {
                    ["engine_type"] = "OpenCV + LBPH",
                    ["engine_version"] = "1.0.0",
                    ["detection_service"] = detectionStats,
                    ["recognition_service"] = recognitionStats,
                    ["configuration"] = new
                    {
                        quality_threshold = _config.QualityThreshold,
                        recognition_threshold = _config.RecognitionThreshold,
                        min_face_size = _config.MinFaceSize,
                        max_face_size = _config.MaxFaceSize,
                        template_size = _config.TemplateSize
                    },
                    ["performance"] = new
                    {
                        target_detection_time_ms = 200,
                        target_recognition_time_ms = 100,
                        target_accuracy_percent = 95
                    }
                };

                return combinedStats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting engine statistics");
                return new Dictionary<string, object>
                {
                    ["error"] = ex.Message,
                    ["engine_status"] = "error"
                };
            }
        }

        public async Task<bool> HealthCheckAsync()
        {
            try
            {
                var detectionHealth = await _faceDetectionService.HealthCheckAsync();
                var recognitionHealth = await _faceRecognitionService.HealthCheckAsync();

                var isHealthy = detectionHealth && recognitionHealth;

                _logger.LogInformation("Face engine health check: detection={DetectionHealth}, recognition={RecognitionHealth}, overall={OverallHealth}",
                    detectionHealth, recognitionHealth, isHealthy);

                return isHealthy;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Face engine health check failed");
                return false;
            }
        }
    }
}