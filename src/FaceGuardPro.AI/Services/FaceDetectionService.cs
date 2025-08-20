using FaceGuardPro.AI.Configuration;
using FaceGuardPro.AI.DTOs;
using FaceGuardPro.AI.Interfaces;
using FaceGuardPro.AI.Models;
using FaceGuardPro.AI.Utilities;
using FaceGuardPro.Core.Interfaces;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using System.Diagnostics;
using System.Drawing;

namespace FaceGuardPro.AI.Services
{
    /// <summary>
    /// OpenCV-based face detection service implementation
    /// </summary>
    public class FaceDetectionService : IFaceDetectionService, IDisposable
    {
        private readonly ILogger<FaceDetectionService> _logger;
        private readonly FaceDetectionConfig _config;
        private readonly CascadeClassifier _faceCascade;
        private readonly CascadeClassifier _eyeCascade;
        private readonly object _lockObject = new();
        private long _processedImages = 0;
        private long _totalProcessingTime = 0;
        private long _detectedFaces = 0;
        private readonly DateTime _serviceStartTime;
        private bool _disposed = false;

        public FaceDetectionConfig Configuration => _config;

        public FaceDetectionService(ILogger<FaceDetectionService> logger, FaceDetectionConfig? config = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? FaceDetectionConfig.Default;
            _serviceStartTime = DateTime.UtcNow;

            try
            {
                // Initialize Haar Cascade models
                FaceDetectionModels.Initialize();

                _faceCascade = new CascadeClassifier(FaceDetectionModels.FrontalFaceDefault);
                _eyeCascade = new CascadeClassifier(FaceDetectionModels.Eye);

                if (_faceCascade.Empty())
                {
                    throw new InvalidOperationException("Failed to load face cascade classifier");
                }

                if (_eyeCascade.Empty())
                {
                    _logger.LogWarning("Failed to load eye cascade classifier, eye detection will be disabled");
                }

                _logger.LogInformation("FaceDetectionService initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize FaceDetectionService");
                throw;
            }
        }

        public async Task<FaceDetectionResult> DetectFacesAsync(
            byte[] imageData,
            FaceProcessingOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new FaceDetectionResult();

            try
            {
                if (imageData == null || imageData.Length == 0)
                {
                    result.ErrorMessage = "Image data is null or empty";
                    return result;
                }

                options ??= new FaceProcessingOptions();

                _logger.LogDebug("Starting face detection for image ({Size} bytes)", imageData.Length);

                // Validate image format
                if (!OpenCvUtilities.IsValidImageFormat(imageData))
                {
                    result.ErrorMessage = "Invalid image format";
                    return result;
                }

                using var originalMat = OpenCvUtilities.BytesToMat(imageData);
                if (originalMat.Empty())
                {
                    result.ErrorMessage = "Failed to decode image";
                    return result;
                }

                result.ImageSize = new System.Drawing.Size(originalMat.Width, originalMat.Height);

                // Preprocess image
                using var processedMat = await PreprocessImageInternalAsync(originalMat, options);

                // Detect faces
                var faces = await DetectFacesInternalAsync(processedMat, options, cancellationToken);

                result.Faces = faces;
                result.FaceCount = faces.Count;
                result.Success = faces.Count > 0;

                // Update statistics
                lock (_lockObject)
                {
                    _processedImages++;
                    _detectedFaces += faces.Count;
                    _totalProcessingTime += stopwatch.ElapsedMilliseconds;
                }

                _logger.LogDebug("Face detection completed: {FaceCount} faces found in {ElapsedMs}ms",
                    faces.Count, stopwatch.ElapsedMilliseconds);
            }
            catch (OperationCanceledException)
            {
                result.ErrorMessage = "Operation was cancelled";
                _logger.LogWarning("Face detection was cancelled");
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Face detection failed: {ex.Message}";
                _logger.LogError(ex, "Error during face detection");
            }
            finally
            {
                stopwatch.Stop();
                result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
            }

            return result;
        }

        public async Task<FaceDetectionResult> DetectFacesFromFileAsync(
            string imagePath,
            FaceProcessingOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!File.Exists(imagePath))
                {
                    return new FaceDetectionResult
                    {
                        ErrorMessage = $"Image file not found: {imagePath}"
                    };
                }

                var imageData = await File.ReadAllBytesAsync(imagePath, cancellationToken);
                return await DetectFacesAsync(imageData, options, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading image file: {ImagePath}", imagePath);
                return new FaceDetectionResult
                {
                    ErrorMessage = $"Failed to read image file: {ex.Message}"
                };
            }
        }

        public async Task<FaceQualityAssessment> AssessFaceQualityAsync(
            byte[] imageData,
            Rectangle faceRegion,
            CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var mat = OpenCvUtilities.BytesToMat(imageData);
                    using var faceMat = OpenCvUtilities.CropFace(mat,
                        OpenCvUtilities.RectangleToRect(faceRegion));

                    return AssessFaceQualityInternal(faceMat, faceRegion);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error assessing face quality");
                    return new FaceQualityAssessment
                    {
                        OverallScore = 0,
                        Factors = new Dictionary<string, double> { ["error"] = 1 }
                    };
                }
            }, cancellationToken);
        }

        public async Task<byte[]> PreprocessImageAsync(
            byte[] imageData,
            int maxWidth = 1024,
            int maxHeight = 768,
            bool enhanceImage = true,
            CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var originalMat = OpenCvUtilities.BytesToMat(imageData);
                    using var resized = OpenCvUtilities.ResizeKeepAspectRatio(originalMat, maxWidth, maxHeight);

                    if (enhanceImage)
                    {
                        using var enhanced = OpenCvUtilities.EnhanceImage(resized);
                        return OpenCvUtilities.MatToBytes(enhanced);
                    }

                    return OpenCvUtilities.MatToBytes(resized);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error preprocessing image");
                    return imageData; // Return original if preprocessing fails
                }
            }, cancellationToken);
        }

        public async Task<bool> HealthCheckAsync()
        {
            try
            {
                // Create a simple test image
                var testImageData = CreateTestImage();
                var result = await DetectFacesAsync(testImageData);

                return !_faceCascade.Empty() && !string.IsNullOrEmpty(result.ErrorMessage) == false;
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
                ["processed_images"] = _processedImages,
                ["detected_faces"] = _detectedFaces,
                ["total_processing_time_ms"] = _totalProcessingTime,
                ["average_processing_time_ms"] = _processedImages > 0 ? _totalProcessingTime / _processedImages : 0,
                ["faces_per_image_avg"] = _processedImages > 0 ? (double)_detectedFaces / _processedImages : 0,
                ["uptime_minutes"] = (DateTime.UtcNow - _serviceStartTime).TotalMinutes,
                ["service_status"] = "running",
                ["face_cascade_loaded"] = !_faceCascade.Empty(),
                ["eye_cascade_loaded"] = !_eyeCascade.Empty(),
                ["configuration"] = new
                {
                    min_face_size = _config.MinFaceSize,
                    max_face_size = _config.MaxFaceSize,
                    scale_factor = _config.ScaleFactor,
                    min_neighbors = _config.MinNeighbors,
                    quality_threshold = _config.QualityThreshold
                }
            });
        }

        private async Task<Mat> PreprocessImageInternalAsync(Mat originalMat, FaceProcessingOptions options)
        {
            return await Task.Run(() =>
            {
                var processed = originalMat.Clone();

                try
                {
                    // Resize if needed
                    if (options.ResizeImage)
                    {
                        var resized = OpenCvUtilities.ResizeKeepAspectRatio(
                            processed, _config.MaxImageWidth, _config.MaxImageHeight);

                        processed.Dispose();
                        processed = resized;
                    }

                    // Enhance image quality
                    if (options.ApplyHistogramEqualization)
                    {
                        var enhanced = OpenCvUtilities.EnhanceImage(processed);
                        processed.Dispose();
                        processed = enhanced;
                    }

                    // Save debug image if enabled
                    if (options.SaveDebugImages && _config.DebugMode)
                    {
                        var fileName = $"preprocessed_{DateTime.Now:yyyyMMdd_HHmmss_fff}.jpg";
                        OpenCvUtilities.SaveDebugImage(processed, fileName, _config.DebugFolder);
                    }

                    return processed;
                }
                catch
                {
                    processed?.Dispose();
                    return originalMat.Clone();
                }
            });
        }

        private async Task<List<DetectedFace>> DetectFacesInternalAsync(
            Mat imageMat,
            FaceProcessingOptions options,
            CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                var detectedFaces = new List<DetectedFace>();

                try
                {
                    // Convert to grayscale for detection
                    using var grayMat = new Mat();
                    if (imageMat.Channels() == 3)
                    {
                        Cv2.CvtColor(imageMat, grayMat, ColorConversionCodes.BGR2GRAY);
                    }
                    else
                    {
                        grayMat = imageMat.Clone();
                    }

                    // Detect faces using Haar Cascade
                    var faceRects = _faceCascade.DetectMultiScale(
                        grayMat,
                        scaleFactor: _config.ScaleFactor,
                        minNeighbors: _config.MinNeighbors,
                        flags: HaarDetectionTypes.ScaleImage,
                        minSize: new OpenCvSharp.Size(_config.MinFaceSize, _config.MinFaceSize),
                        maxSize: _config.MaxFaceSize > 0 ?
                            new OpenCvSharp.Size(_config.MaxFaceSize, _config.MaxFaceSize) :
                            new OpenCvSharp.Size()
                    );

                    _logger.LogDebug("Haar cascade detected {FaceCount} potential faces", faceRects.Length);

                    foreach (var faceRect in faceRects)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        var detectedFace = ProcessDetectedFace(imageMat, grayMat, faceRect, options);

                        if (detectedFace.QualityScore >= _config.QualityThreshold)
                        {
                            detectedFaces.Add(detectedFace);
                        }

                        // If single face mode, break after first good face
                        if (!options.DetectMultipleFaces && detectedFaces.Count > 0)
                            break;
                    }

                    // Sort by quality score (best first)
                    detectedFaces.Sort((a, b) => b.QualityScore.CompareTo(a.QualityScore));

                    return detectedFaces;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during face detection");
                    return detectedFaces;
                }
            }, cancellationToken);
        }

        private DetectedFace ProcessDetectedFace(Mat imageMat, Mat grayMat, Rect faceRect, FaceProcessingOptions options)
        {
            var detectedFace = new DetectedFace
            {
                BoundingBox = OpenCvUtilities.RectToRectangle(faceRect),
                Confidence = 85.0 // Haar cascade doesn't provide confidence, use default
            };

            try
            {
                // Assess face quality
                if (options.CalculateQuality)
                {
                    using var faceMat = OpenCvUtilities.CropFace(imageMat, faceRect);
                    var quality = AssessFaceQualityInternal(faceMat, detectedFace.BoundingBox);
                    detectedFace.QualityScore = quality.OverallScore;
                }

                // Detect eyes for better confidence assessment
                if (!_eyeCascade.Empty())
                {
                    using var faceRegion = grayMat[faceRect];
                    var eyeRects = _eyeCascade.DetectMultiScale(faceRegion, 1.1, 3);

                    if (eyeRects.Length >= 2)
                    {
                        detectedFace.Confidence = Math.Min(95.0, detectedFace.Confidence + 10.0);
                    }
                }

                // Calculate face size category
                detectedFace.SizeCategory = CalculateFaceSizeCategory(faceRect);

                // Add metadata
                detectedFace.Metadata["face_width"] = faceRect.Width;
                detectedFace.Metadata["face_height"] = faceRect.Height;
                detectedFace.Metadata["face_area"] = faceRect.Width * faceRect.Height;
                detectedFace.Metadata["detection_method"] = "haar_cascade";

                return detectedFace;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error processing detected face");
                detectedFace.QualityScore = 50.0; // Default poor quality
                return detectedFace;
            }
        }

        private FaceQualityAssessment AssessFaceQualityInternal(Mat faceMat, Rectangle faceRegion)
        {
            var assessment = new FaceQualityAssessment();

            try
            {
                // Assess brightness and contrast
                var (brightness, contrast) = OpenCvUtilities.AssessImageQuality(faceMat);
                assessment.BrightnessScore = NormalizeBrightnessScore(brightness);
                assessment.ContrastScore = NormalizeContrastScore(contrast);

                // Assess sharpness
                assessment.SharpnessScore = OpenCvUtilities.AssessSharpness(faceMat);

                // Assess face size
                assessment.SizeScore = AssessFaceSize(faceRegion);

                // Calculate overall score (weighted average)
                assessment.OverallScore =
                    (assessment.BrightnessScore * 0.2) +
                    (assessment.ContrastScore * 0.2) +
                    (assessment.SharpnessScore * 0.3) +
                    (assessment.SizeScore * 0.3);

                // Store individual factors
                assessment.Factors["brightness"] = assessment.BrightnessScore;
                assessment.Factors["contrast"] = assessment.ContrastScore;
                assessment.Factors["sharpness"] = assessment.SharpnessScore;
                assessment.Factors["size"] = assessment.SizeScore;
                assessment.Factors["overall"] = assessment.OverallScore;

                return assessment;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error assessing face quality");
                assessment.OverallScore = 50.0;
                return assessment;
            }
        }

        private double NormalizeBrightnessScore(double brightness)
        {
            // Optimal brightness range: 40-80%
            if (brightness >= 40 && brightness <= 80)
                return 100.0;
            else if (brightness < 40)
                return Math.Max(0, brightness / 40.0 * 100);
            else
                return Math.Max(0, (100 - brightness) / 20.0 * 100);
        }

        private double NormalizeContrastScore(double contrast)
        {
            // Good contrast should be above 20%
            return Math.Min(100.0, contrast / 30.0 * 100);
        }

        private double AssessFaceSize(Rectangle faceRegion)
        {
            var area = faceRegion.Width * faceRegion.Height;

            // Optimal face size: 80x80 to 300x300 pixels
            if (area >= 6400 && area <= 90000) // 80x80 to 300x300
                return 100.0;
            else if (area < 6400)
                return Math.Max(20.0, area / 6400.0 * 100);
            else
                return Math.Max(50.0, 100.0 - (area - 90000) / 90000.0 * 50);
        }

        private FaceSizeCategory CalculateFaceSizeCategory(Rect faceRect)
        {
            var area = faceRect.Width * faceRect.Height;

            return area switch
            {
                < 3600 => FaceSizeCategory.VerySmall,    // < 60x60
                < 6400 => FaceSizeCategory.Small,        // < 80x80
                < 22500 => FaceSizeCategory.Medium,      // < 150x150
                < 62500 => FaceSizeCategory.Large,       // < 250x250
                _ => FaceSizeCategory.VeryLarge          // >= 250x250
            };
        }

        private byte[] CreateTestImage()
        {
            // Create a simple 100x100 test image for health check
            using var testMat = Mat.Zeros(100, 100, MatType.CV_8UC3);
            return OpenCvUtilities.MatToBytes(testMat);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _faceCascade?.Dispose();
                _eyeCascade?.Dispose();
                _disposed = true;
                _logger.LogInformation("FaceDetectionService disposed");
            }
        }
    }
}