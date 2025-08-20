using FaceGuardPro.AI.Configuration;
using FaceGuardPro.AI.DTOs;
using FaceGuardPro.AI.Interfaces;
using FaceGuardPro.AI.Utilities;
using FaceGuardPro.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenCvSharp;
using OpenCvSharp.Face;
using System.Diagnostics;
using System.Drawing;
using System.Text;

namespace FaceGuardPro.AI.Services
{
    /// <summary>
    /// LBPH-based face recognition service implementation
    /// </summary>
    public class FaceRecognitionService : IFaceRecognitionService, IDisposable
    {
        private readonly ILogger<FaceRecognitionService> _logger;
        private readonly FaceDetectionConfig _config;
        private readonly IFaceDetectionService _faceDetectionService;
        private readonly FaceRecognizer _recognizer;
        private readonly object _lockObject = new();
        private long _generatedTemplates = 0;
        private long _comparisons = 0;
        private long _totalProcessingTime = 0;
        private readonly DateTime _serviceStartTime;
        private bool _disposed = false;

        public FaceRecognitionService(
            ILogger<FaceRecognitionService> logger,
            IFaceDetectionService faceDetectionService,
            FaceDetectionConfig? config = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _faceDetectionService = faceDetectionService ?? throw new ArgumentNullException(nameof(faceDetectionService));
            _config = config ?? FaceDetectionConfig.Default;
            _serviceStartTime = DateTime.UtcNow;

            try
            {
                // Initialize LBPH Face Recognizer
                _recognizer = LBPHFaceRecognizer.Create(
                    radius: _config.LBPHRadius,
                    neighbors: _config.LBPHNeighbors,
                    gridX: _config.LBPHGridX,
                    gridY: _config.LBPHGridY,
                    threshold: 100.0 // We'll handle threshold manually for better control
                );

                _logger.LogInformation("FaceRecognitionService initialized successfully with LBPH parameters: " +
                    "radius={Radius}, neighbors={Neighbors}, gridX={GridX}, gridY={GridY}",
                    _config.LBPHRadius, _config.LBPHNeighbors, _config.LBPHGridX, _config.LBPHGridY);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize FaceRecognitionService");
                throw;
            }
        }

        public async Task<FaceTemplateResult> GenerateTemplateAsync(
            byte[] imageData,
            Rectangle? faceRegion = null,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new FaceTemplateResult();

            try
            {
                if (imageData == null || imageData.Length == 0)
                {
                    result.ErrorMessage = "Image data is null or empty";
                    return result;
                }

                _logger.LogDebug("Starting template generation for image ({Size} bytes)", imageData.Length);

                // Detect face if region not provided
                DetectedFace? detectedFace = null;
                if (faceRegion.HasValue)
                {
                    detectedFace = new DetectedFace { BoundingBox = faceRegion.Value };
                }
                else
                {
                    var detectionResult = await _faceDetectionService.DetectFacesAsync(imageData,
                        new FaceProcessingOptions { DetectMultipleFaces = false }, cancellationToken);

                    if (!detectionResult.Success || detectionResult.Faces.Count == 0)
                    {
                        result.ErrorMessage = "No face detected in image";
                        return result;
                    }

                    detectedFace = detectionResult.Faces.First();
                }

                // Generate template
                var templateData = await GenerateTemplateInternalAsync(imageData, detectedFace, cancellationToken);

                if (templateData != null && templateData.Length > 0)
                {
                    result.Success = true;
                    result.TemplateData = templateData;
                    result.TemplateSize = templateData.Length;
                    result.QualityScore = detectedFace.QualityScore;
                    result.SourceFace = detectedFace;
                    result.Version = "LBPH_1.0";

                    lock (_lockObject)
                    {
                        _generatedTemplates++;
                        _totalProcessingTime += stopwatch.ElapsedMilliseconds;
                    }

                    _logger.LogDebug("Template generated successfully: {Size} bytes, quality: {Quality}",
                        templateData.Length, detectedFace.QualityScore);
                }
                else
                {
                    result.ErrorMessage = "Failed to generate face template";
                }
            }
            catch (OperationCanceledException)
            {
                result.ErrorMessage = "Operation was cancelled";
                _logger.LogWarning("Template generation was cancelled");
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Template generation failed: {ex.Message}";
                _logger.LogError(ex, "Error during template generation");
            }
            finally
            {
                stopwatch.Stop();
                result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
            }

            return result;
        }

        public async Task<FaceComparisonResult> CompareTemplatesAsync(
            byte[] template1,
            byte[] template2,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new FaceComparisonResult();

            try
            {
                if (template1 == null || template1.Length == 0 ||
                    template2 == null || template2.Length == 0)
                {
                    result.ErrorMessage = "Invalid template data";
                    return result;
                }

                _logger.LogDebug("Comparing templates ({Size1} bytes vs {Size2} bytes)",
                    template1.Length, template2.Length);

                var comparison = await CompareTemplatesInternalAsync(template1, template2, cancellationToken);

                result.Success = true;
                result.SimilarityScore = comparison.similarity;
                result.Distance = comparison.distance;
                result.Confidence = comparison.confidence;
                result.IsMatch = comparison.similarity >= _config.RecognitionThreshold;

                result.Metadata["template1_size"] = template1.Length;
                result.Metadata["template2_size"] = template2.Length;
                result.Metadata["threshold_used"] = _config.RecognitionThreshold;

                lock (_lockObject)
                {
                    _comparisons++;
                }

                _logger.LogDebug("Template comparison completed: similarity={Similarity}%, distance={Distance}, match={IsMatch}",
                    result.SimilarityScore, result.Distance, result.IsMatch);
            }
            catch (OperationCanceledException)
            {
                result.ErrorMessage = "Operation was cancelled";
                _logger.LogWarning("Template comparison was cancelled");
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Template comparison failed: {ex.Message}";
                _logger.LogError(ex, "Error during template comparison");
            }
            finally
            {
                stopwatch.Stop();
                result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
            }

            return result;
        }

        public async Task<FaceComparisonResult> CompareWithTemplateAsync(
            byte[] imageData,
            byte[] template,
            Rectangle? faceRegion = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Generate template from image
                var templateResult = await GenerateTemplateAsync(imageData, faceRegion, cancellationToken);

                if (!templateResult.Success || templateResult.TemplateData == null)
                {
                    return new FaceComparisonResult
                    {
                        ErrorMessage = $"Failed to generate template from image: {templateResult.ErrorMessage}"
                    };
                }

                // Compare templates
                return await CompareTemplatesAsync(templateResult.TemplateData, template, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error comparing image with template");
                return new FaceComparisonResult
                {
                    ErrorMessage = $"Comparison failed: {ex.Message}"
                };
            }
        }

        public async Task<List<FaceMatchResult>> CompareWithMultipleTemplatesAsync(
            byte[] imageData,
            Dictionary<string, byte[]> templates,
            double threshold = 80.0,
            CancellationToken cancellationToken = default)
        {
            var results = new List<FaceMatchResult>();

            try
            {
                if (templates == null || templates.Count == 0)
                {
                    return results;
                }

                // Generate template from query image
                var queryTemplateResult = await GenerateTemplateAsync(imageData, cancellationToken: cancellationToken);

                if (!queryTemplateResult.Success || queryTemplateResult.TemplateData == null)
                {
                    _logger.LogWarning("Failed to generate template from query image: {Error}", queryTemplateResult.ErrorMessage);
                    return results;
                }

                _logger.LogDebug("Comparing with {Count} stored templates", templates.Count);

                // Compare with each stored template
                foreach (var kvp in templates)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    try
                    {
                        var comparison = await CompareTemplatesAsync(
                            queryTemplateResult.TemplateData,
                            kvp.Value,
                            cancellationToken);

                        if (comparison.Success)
                        {
                            results.Add(new FaceMatchResult
                            {
                                TemplateId = kvp.Key,
                                SimilarityScore = comparison.SimilarityScore,
                                Distance = comparison.Distance,
                                Confidence = comparison.Confidence,
                                IsMatch = comparison.SimilarityScore >= threshold,
                                Metadata = comparison.Metadata
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error comparing with template {TemplateId}", kvp.Key);
                    }
                }

                // Sort by similarity score (best first) and assign ranks
                results.Sort((a, b) => b.SimilarityScore.CompareTo(a.SimilarityScore));
                for (int i = 0; i < results.Count; i++)
                {
                    results[i].Rank = i + 1;
                }

                _logger.LogDebug("Multi-template comparison completed: {ResultCount} results, {MatchCount} matches",
                    results.Count, results.Count(r => r.IsMatch));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during multi-template comparison");
            }

            return results;
        }

        public bool IsValidTemplate(byte[] templateData)
        {
            try
            {
                if (templateData == null || templateData.Length == 0)
                    return false;

                var templateInfo = DeserializeTemplate(templateData);
                return templateInfo != null && templateInfo.IsValid;
            }
            catch
            {
                return false;
            }
        }

        public FaceTemplateInfo GetTemplateInfo(byte[] templateData)
        {
            try
            {
                if (templateData == null || templateData.Length == 0)
                {
                    return new FaceTemplateInfo { IsValid = false };
                }

                var templateInfo = DeserializeTemplate(templateData);
                return templateInfo ?? new FaceTemplateInfo { IsValid = false };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting template info");
                return new FaceTemplateInfo
                {
                    IsValid = false,
                    Metadata = new Dictionary<string, object> { ["error"] = ex.Message }
                };
            }
        }

        public async Task<FaceTemplateResult> GenerateBestTemplateAsync(
            List<byte[]> faceImages,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (faceImages == null || faceImages.Count == 0)
                {
                    return new FaceTemplateResult
                    {
                        ErrorMessage = "No face images provided"
                    };
                }

                _logger.LogDebug("Generating best template from {Count} face images", faceImages.Count);

                var templates = new List<(FaceTemplateResult result, double quality)>();

                // Generate template for each image
                foreach (var imageData in faceImages)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    var templateResult = await GenerateTemplateAsync(imageData, cancellationToken: cancellationToken);
                    if (templateResult.Success && templateResult.SourceFace != null)
                    {
                        templates.Add((templateResult, templateResult.SourceFace.QualityScore));
                    }
                }

                if (templates.Count == 0)
                {
                    return new FaceTemplateResult
                    {
                        ErrorMessage = "Failed to generate any valid templates"
                    };
                }

                // Return the template with the highest quality score
                var bestTemplate = templates.OrderByDescending(t => t.quality).First();

                _logger.LogDebug("Best template selected with quality score: {Quality}",
                    bestTemplate.quality);

                return bestTemplate.result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating best template");
                return new FaceTemplateResult
                {
                    ErrorMessage = $"Failed to generate best template: {ex.Message}"
                };
            }
        }

        public async Task<bool> HealthCheckAsync()
        {
            try
            {
                // Create test templates and compare them
                var testImageData = CreateTestFaceImage();
                var templateResult = await GenerateTemplateAsync(testImageData);

                if (!templateResult.Success || templateResult.TemplateData == null)
                    return false;

                var comparisonResult = await CompareTemplatesAsync(
                    templateResult.TemplateData,
                    templateResult.TemplateData);

                return comparisonResult.Success && comparisonResult.SimilarityScore > 95;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                return false;
            }
        }

        public async Task<Dictionary<string, object>> GetStatisticsAsync()
        {
            return await Task.FromResult(new Dictionary<string, object>
            {
                ["generated_templates"] = _generatedTemplates,
                ["comparisons_performed"] = _comparisons,
                ["total_processing_time_ms"] = _totalProcessingTime,
                ["average_processing_time_ms"] = _generatedTemplates > 0 ? _totalProcessingTime / _generatedTemplates : 0,
                ["uptime_minutes"] = (DateTime.UtcNow - _serviceStartTime).TotalMinutes,
                ["service_status"] = "running",
                ["recognition_threshold"] = _config.RecognitionThreshold,
                ["lbph_parameters"] = new
                {
                    radius = _config.LBPHRadius,
                    neighbors = _config.LBPHNeighbors,
                    grid_x = _config.LBPHGridX,
                    grid_y = _config.LBPHGridY
                }
            });
        }

        private async Task<byte[]?> GenerateTemplateInternalAsync(
            byte[] imageData,
            DetectedFace detectedFace,
            CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var originalMat = OpenCvUtilities.BytesToMat(imageData);
                    using var grayMat = new Mat();

                    // Convert to grayscale
                    if (originalMat.Channels() == 3)
                    {
                        Cv2.CvtColor(originalMat, grayMat, ColorConversionCodes.BGR2GRAY);
                    }
                    else
                    {
                        grayMat = originalMat.Clone();
                    }

                    // Crop face region
                    var faceRect = OpenCvUtilities.RectangleToRect(detectedFace.BoundingBox);
                    using var faceMat = OpenCvUtilities.CropFace(grayMat, faceRect, 0.1);

                    // Resize to standard template size
                    using var resizedMat = new Mat();
                    Cv2.Resize(faceMat, resizedMat, new OpenCvSharp.Size(_config.TemplateSize, _config.TemplateSize));

                    // Apply histogram equalization for better consistency
                    using var equalizedMat = new Mat();
                    Cv2.EqualizeHist(resizedMat, equalizedMat);

                    // Create template data structure
                    var templateData = new FaceTemplate
                    {
                        Version = "LBPH_1.0",
                        CreatedAt = DateTime.UtcNow,
                        QualityScore = detectedFace.QualityScore,
                        Size = _config.TemplateSize,
                        ImageData = OpenCvUtilities.MatToBytes(equalizedMat, ".png"), // Use PNG for lossless compression
                        Metadata = new Dictionary<string, object>
                        {
                            ["lbph_radius"] = _config.LBPHRadius,
                            ["lbph_neighbors"] = _config.LBPHNeighbors,
                            ["lbph_grid_x"] = _config.LBPHGridX,
                            ["lbph_grid_y"] = _config.LBPHGridY,
                            ["original_face_size"] = $"{detectedFace.BoundingBox.Width}x{detectedFace.BoundingBox.Height}",
                            ["template_size"] = _config.TemplateSize
                        }
                    };

                    return SerializeTemplate(templateData);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating template internally");
                    return null;
                }
            }, cancellationToken);
        }

        private async Task<(double similarity, double distance, double confidence)> CompareTemplatesInternalAsync(
            byte[] template1,
            byte[] template2,
            CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var templateData1 = DeserializeTemplate(template1);
                    var templateData2 = DeserializeTemplate(template2);

                    if (templateData1 == null || templateData2 == null)
                    {
                        throw new ArgumentException("Invalid template data");
                    }

                    using var mat1 = OpenCvUtilities.BytesToMat(templateData1.ImageData);
                    using var mat2 = OpenCvUtilities.BytesToMat(templateData2.ImageData);

                    // Ensure both images are grayscale and same size
                    using var gray1 = mat1.Channels() == 3 ?
                        mat1.CvtColor(ColorConversionCodes.BGR2GRAY) : mat1.Clone();
                    using var gray2 = mat2.Channels() == 3 ?
                        mat2.CvtColor(ColorConversionCodes.BGR2GRAY) : mat2.Clone();

                    // Resize to same dimensions if needed
                    if (gray1.Size() != gray2.Size())
                    {
                        using var resized2 = new Mat();
                        Cv2.Resize(gray2, resized2, gray1.Size());
                        gray2.Dispose();
                        gray2 = resized2.Clone();
                    }

                    // Calculate histogram-based similarity
                    var histSimilarity = CalculateHistogramSimilarity(gray1, gray2);

                    // Calculate template matching similarity
                    var templateSimilarity = CalculateTemplateSimilarity(gray1, gray2);

                    // Calculate SSIM (Structural Similarity Index)
                    var ssimSimilarity = CalculateSSIMSimilarity(gray1, gray2);

                    // Combine different similarity measures (weighted average)
                    var combinedSimilarity =
                        (histSimilarity * 0.3) +
                        (templateSimilarity * 0.4) +
                        (ssimSimilarity * 0.3);

                    // Calculate distance (inverse of similarity)
                    var distance = 100.0 - combinedSimilarity;

                    // Calculate confidence based on consistency of different measures
                    var measures = new[] { histSimilarity, templateSimilarity, ssimSimilarity };
                    var avgSimilarity = measures.Average();
                    var variance = measures.Select(m => Math.Pow(m - avgSimilarity, 2)).Average();
                    var confidence = Math.Max(0, 100.0 - Math.Sqrt(variance) * 10);

                    return (combinedSimilarity, distance, confidence);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error comparing templates internally");
                    return (0.0, 100.0, 0.0);
                }
            }, cancellationToken);
        }

        private double CalculateHistogramSimilarity(Mat image1, Mat image2)
        {
            try
            {
                using var hist1 = new Mat();
                using var hist2 = new Mat();

                var histSize = new[] { 256 };
                var ranges = new[] { new Rangef(0, 256) };

                Cv2.CalcHist(new[] { image1 }, new[] { 0 }, null, hist1, 1, histSize, ranges);
                Cv2.CalcHist(new[] { image2 }, new[] { 0 }, null, hist2, 1, histSize, ranges);

                var correlation = Cv2.CompareHist(hist1, hist2, HistCompMethods.Correl);
                return Math.Max(0, correlation * 100);
            }
            catch
            {
                return 0.0;
            }
        }

        private double CalculateTemplateSimilarity(Mat image1, Mat image2)
        {
            try
            {
                using var result = new Mat();
                Cv2.MatchTemplate(image1, image2, result, TemplateMatchModes.CCoeffNormed);

                Cv2.MinMaxLoc(result, out _, out var maxVal);
                return Math.Max(0, maxVal * 100);
            }
            catch
            {
                return 0.0;
            }
        }

        private double CalculateSSIMSimilarity(Mat image1, Mat image2)
        {
            try
            {
                // Convert to float for calculations
                using var float1 = new Mat();
                using var float2 = new Mat();
                image1.ConvertTo(float1, MatType.CV_64F);
                image2.ConvertTo(float2, MatType.CV_64F);

                // Calculate means
                var mu1 = Cv2.Mean(float1).Val0;
                var mu2 = Cv2.Mean(float2).Val0;

                // Calculate variances and covariance
                using var diff1 = float1 - mu1;
                using var diff2 = float2 - mu2;

                var sigma1_sq = Cv2.Mean(diff1.Mul(diff1)).Val0;
                var sigma2_sq = Cv2.Mean(diff2.Mul(diff2)).Val0;
                var sigma12 = Cv2.Mean(diff1.Mul(diff2)).Val0;

                // SSIM constants
                const double c1 = 6.5025;   // (0.01 * 255)^2
                const double c2 = 58.5225;  // (0.03 * 255)^2

                // Calculate SSIM
                var numerator = (2 * mu1 * mu2 + c1) * (2 * sigma12 + c2);
                var denominator = (mu1 * mu1 + mu2 * mu2 + c1) * (sigma1_sq + sigma2_sq + c2);

                var ssim = numerator / denominator;
                return Math.Max(0, Math.Min(100, ssim * 100));
            }
            catch
            {
                return 0.0;
            }
        }

        private byte[] SerializeTemplate(FaceTemplate template)
        {
            var json = JsonConvert.SerializeObject(template, Formatting.None);
            return Encoding.UTF8.GetBytes(json);
        }

        private FaceTemplate? DeserializeTemplate(byte[] templateData)
        {
            try
            {
                var json = Encoding.UTF8.GetString(templateData);
                return JsonConvert.DeserializeObject<FaceTemplate>(json);
            }
            catch
            {
                return null;
            }
        }

        private byte[] CreateTestFaceImage()
        {
            // Create a simple test face image (circle with two dots for eyes)
            using var testMat = Mat.Zeros(100, 100, MatType.CV_8UC1);

            // Draw face circle
            Cv2.Circle(testMat, new OpenCvSharp.Point(50, 50), 40, Scalar.White, -1);

            // Draw eyes
            Cv2.Circle(testMat, new OpenCvSharp.Point(35, 40), 5, Scalar.Black, -1);
            Cv2.Circle(testMat, new OpenCvSharp.Point(65, 40), 5, Scalar.Black, -1);

            // Draw mouth
            Cv2.Ellipse(testMat, new OpenCvSharp.Point(50, 65), new OpenCvSharp.Size(15, 8), 0, 0, 180, Scalar.Black, 2);

            return OpenCvUtilities.MatToBytes(testMat);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _recognizer?.Dispose();
                _disposed = true;
                _logger.LogInformation("FaceRecognitionService disposed");
            }
        }

        /// <summary>
        /// Internal face template structure for serialization
        /// </summary>
        private class FaceTemplate
        {
            public string Version { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
            public double QualityScore { get; set; }
            public int Size { get; set; }
            public byte[] ImageData { get; set; } = Array.Empty<byte>();
            public Dictionary<string, object> Metadata { get; set; } = new();
            public bool IsValid => !string.IsNullOrEmpty(Version) && ImageData.Length > 0;
        }
    }
}