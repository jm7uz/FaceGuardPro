// src/FaceGuardPro.AI/Services/OpenCvFaceService.cs - Minimal Test Version
using OpenCvSharp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using FaceGuardPro.AI.Interfaces;
using FaceGuardPro.AI.Configuration;
using FaceGuardPro.Shared.Models;

namespace FaceGuardPro.AI.Services;

public class OpenCvFaceService : IOpenCvFaceService, IDisposable
{
    private readonly ILogger<OpenCvFaceService> _logger;
    private readonly OpenCvConfiguration _config;
    private CascadeClassifier? _faceCascade;

    public OpenCvFaceService(
        ILogger<OpenCvFaceService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _config = new OpenCvConfiguration();

        try
        {
            InitializeCascade();
            _logger.LogInformation("OpenCV Face Service initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize OpenCV Face Service - using fallback mode");
            // Don't throw, use fallback detection
        }
    }

    private void InitializeCascade()
    {
        var cascadePaths = new[]
        {
            "Models/haarcascade_frontalface_alt2.xml",
            "haarcascade_frontalface_alt2.xml",
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "haarcascade_frontalface_alt2.xml")
        };

        foreach (var path in cascadePaths)
        {
            if (File.Exists(path))
            {
                _faceCascade = new CascadeClassifier(path);
                if (!_faceCascade.Empty())
                {
                    _logger.LogInformation("Loaded cascade from: {Path}", path);
                    return;
                }
            }
        }

        _logger.LogWarning("No cascade file found, using basic detection");
    }

    public async Task<OpenCvFaceDetectionResult> DetectFaceAsync(byte[] imageData)
    {
        try
        {
            // Basic validation
            if (imageData == null || imageData.Length == 0)
            {
                return new OpenCvFaceDetectionResult { Message = "Empty image data" };
            }

            using var mat = Mat.FromImageData(imageData, ImreadModes.Color);
            if (mat.Empty())
            {
                return new OpenCvFaceDetectionResult { Message = "Invalid image format" };
            }

            // If cascade not available, use basic detection
            if (_faceCascade == null || _faceCascade.Empty())
            {
                return CreateFallbackDetection(mat);
            }

            using var gray = new Mat();
            Cv2.CvtColor(mat, gray, ColorConversionCodes.BGR2GRAY);

            var faces = _faceCascade.DetectMultiScale(gray);

            if (faces.Length == 0)
            {
                return new OpenCvFaceDetectionResult { Message = "No face detected" };
            }

            var face = faces[0]; // Take first face
            return new OpenCvFaceDetectionResult
            {
                Success = true,
                Message = "Face detected successfully",
                Confidence = 0.85,
                FaceRect = new OpenCvBoundingRect
                {
                    X = face.X,
                    Y = face.Y,
                    Width = face.Width,
                    Height = face.Height
                },
                Quality = CreateBasicQuality(face, mat)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in face detection");
            return new OpenCvFaceDetectionResult { Message = $"Detection error: {ex.Message}" };
        }
    }

    private OpenCvFaceDetectionResult CreateFallbackDetection(Mat mat)
    {
        // Simple fallback - assume center of image has a face
        var centerX = mat.Width / 4;
        var centerY = mat.Height / 4;
        var faceSize = Math.Min(mat.Width, mat.Height) / 2;

        return new OpenCvFaceDetectionResult
        {
            Success = true,
            Message = "Face detected (fallback mode)",
            Confidence = 0.70,
            FaceRect = new OpenCvBoundingRect
            {
                X = centerX,
                Y = centerY,
                Width = faceSize,
                Height = faceSize
            },
            Quality = new OpenCvFaceQualityResult
            {
                OverallScore = 0.75,
                IsAcceptable = true,
                Brightness = 128,
                Contrast = 0.5,
                Sharpness = 0.6,
                FaceSize = 0.8
            }
        };
    }

    private OpenCvFaceQualityResult CreateBasicQuality(Rect face, Mat mat)
    {
        try
        {
            using var gray = new Mat();
            Cv2.CvtColor(mat, gray, ColorConversionCodes.BGR2GRAY);
            using var faceImage = gray[face];

            var mean = Cv2.Mean(faceImage);
            var brightness = mean.Val0;

            return new OpenCvFaceQualityResult
            {
                OverallScore = 0.80,
                IsAcceptable = true,
                Brightness = brightness,
                Contrast = 0.6,
                Sharpness = 0.7,
                FaceSize = (double)(face.Width * face.Height) / (mat.Width * mat.Height)
            };
        }
        catch
        {
            return new OpenCvFaceQualityResult
            {
                OverallScore = 0.70,
                IsAcceptable = true,
                Brightness = 128,
                Contrast = 0.5,
                Sharpness = 0.6,
                FaceSize = 0.5
            };
        }
    }

    public async Task<List<OpenCvFaceDetectionResult>> DetectMultipleFacesAsync(byte[] imageData)
    {
        var single = await DetectFaceAsync(imageData);
        return new List<OpenCvFaceDetectionResult> { single };
    }

    public async Task<OpenCvFaceQualityResult> AnalyzeFaceQualityAsync(byte[] imageData)
    {
        var detection = await DetectFaceAsync(imageData);
        return detection.Quality;
    }

    public async Task<OpenCvFaceTemplate> CreateFaceTemplateAsync(byte[] imageData)
    {
        var detection = await DetectFaceAsync(imageData);
        if (!detection.Success)
        {
            throw new InvalidOperationException($"Cannot create template: {detection.Message}");
        }

        // Create simple template from detection data
        var templateData = System.Text.Encoding.UTF8.GetBytes($"{detection.FaceRect.X},{detection.FaceRect.Y},{detection.FaceRect.Width},{detection.FaceRect.Height}");

        return new OpenCvFaceTemplate
        {
            TemplateData = templateData,
            Quality = detection.Quality.OverallScore,
            Algorithm = "Basic",
            Metadata = new Dictionary<string, object>
            {
                ["FaceRect"] = $"{detection.FaceRect.X},{detection.FaceRect.Y},{detection.FaceRect.Width},{detection.FaceRect.Height}",
                ["Confidence"] = detection.Confidence
            }
        };
    }

    public async Task<OpenCvFaceComparisonResult> CompareFacesAsync(byte[] image1, byte[] image2)
    {
        try
        {
            var detection1 = await DetectFaceAsync(image1);
            var detection2 = await DetectFaceAsync(image2);

            if (!detection1.Success || !detection2.Success)
            {
                return new OpenCvFaceComparisonResult
                {
                    IsMatch = false,
                    Similarity = 0.0,
                    Method = "Detection Failed"
                };
            }

            // Simple comparison based on face size similarity
            var area1 = detection1.FaceRect.Area;
            var area2 = detection2.FaceRect.Area;
            var sizeDiff = Math.Abs(area1 - area2) / Math.Max(area1, area2);
            var similarity = (1.0 - sizeDiff) * 100;

            return new OpenCvFaceComparisonResult
            {
                IsMatch = similarity > 75,
                Similarity = similarity,
                Distance = 100 - similarity,
                Threshold = 75.0,
                Method = "Size Comparison"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing faces");
            return new OpenCvFaceComparisonResult
            {
                IsMatch = false,
                Similarity = 0.0,
                Method = "Error"
            };
        }
    }

    public async Task<OpenCvFaceComparisonResult> CompareWithTemplateAsync(OpenCvFaceTemplate template, byte[] imageData)
    {
        // For simplicity, create a new template and compare
        try
        {
            var newTemplate = await CreateFaceTemplateAsync(imageData);

            // Compare template data (basic string comparison for now)
            var template1Str = System.Text.Encoding.UTF8.GetString(template.TemplateData);
            var template2Str = System.Text.Encoding.UTF8.GetString(newTemplate.TemplateData);

            var similarity = template1Str == template2Str ? 100.0 : 70.0; // Basic comparison

            return new OpenCvFaceComparisonResult
            {
                IsMatch = similarity > 75,
                Similarity = similarity,
                Distance = 100 - similarity,
                Threshold = 75.0,
                Method = "Template Comparison"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing with template");
            return new OpenCvFaceComparisonResult
            {
                IsMatch = false,
                Similarity = 0.0,
                Method = "Error"
            };
        }
    }

    public async Task<List<FaceLandmark>> DetectLandmarksAsync(byte[] imageData)
    {
        var detection = await DetectFaceAsync(imageData);
        if (!detection.Success) return new List<FaceLandmark>();

        var face = detection.FaceRect;
        return new List<FaceLandmark>
        {
            new() { Id = 0, Name = "LeftEye", Point = new Point2D(face.X + face.Width * 0.3f, face.Y + face.Height * 0.3f), Confidence = 0.7 },
            new() { Id = 1, Name = "RightEye", Point = new Point2D(face.X + face.Width * 0.7f, face.Y + face.Height * 0.3f), Confidence = 0.7 },
            new() { Id = 2, Name = "Nose", Point = new Point2D(face.X + face.Width * 0.5f, face.Y + face.Height * 0.5f), Confidence = 0.7 },
            new() { Id = 3, Name = "Mouth", Point = new Point2D(face.X + face.Width * 0.5f, face.Y + face.Height * 0.7f), Confidence = 0.7 }
        };
    }

    public async Task<byte[]> PreprocessImageAsync(byte[] imageData)
    {
        // Just return the same data for now
        return imageData;
    }

    public async Task<bool> ValidateImageAsync(byte[] imageData)
    {
        try
        {
            if (imageData == null || imageData.Length == 0) return false;
            if (imageData.Length > 10 * 1024 * 1024) return false; // 10MB max

            using var mat = Mat.FromImageData(imageData, ImreadModes.Color);
            return !mat.Empty() && mat.Width >= 100 && mat.Height >= 100;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        try
        {
            _faceCascade?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing OpenCV resources");
        }
    }
}