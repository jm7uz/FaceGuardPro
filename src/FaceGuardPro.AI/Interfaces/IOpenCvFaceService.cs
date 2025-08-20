// src/FaceGuardPro.AI/Interfaces/IOpenCvFaceService.cs
using FaceGuardPro.Shared.Models;

namespace FaceGuardPro.AI.Interfaces;

public interface IOpenCvFaceService
{
    // Face Detection
    Task<OpenCvFaceDetectionResult> DetectFaceAsync(byte[] imageData);
    Task<List<OpenCvFaceDetectionResult>> DetectMultipleFacesAsync(byte[] imageData);
    Task<OpenCvFaceQualityResult> AnalyzeFaceQualityAsync(byte[] imageData);

    // Face Template Management (without LBPH recognizer)
    Task<OpenCvFaceTemplate> CreateFaceTemplateAsync(byte[] imageData);
    Task<OpenCvFaceComparisonResult> CompareFacesAsync(byte[] image1, byte[] image2);
    Task<OpenCvFaceComparisonResult> CompareWithTemplateAsync(OpenCvFaceTemplate template, byte[] imageData);

    // Face Landmarks (basic approximation)
    Task<List<FaceLandmark>> DetectLandmarksAsync(byte[] imageData);

    // Utility Methods
    Task<byte[]> PreprocessImageAsync(byte[] imageData);
    Task<bool> ValidateImageAsync(byte[] imageData);
}

// OpenCV specific models
public class OpenCvFaceDetectionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public OpenCvBoundingRect FaceRect { get; set; } = new();
    public List<FaceLandmark> Landmarks { get; set; } = new();
    public OpenCvFaceQualityResult Quality { get; set; } = new();
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

public class OpenCvBoundingRect
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public double Area => Width * Height;
    public Point2D Center => new((float)(X + Width / 2), (float)(Y + Height / 2));
}

public class OpenCvFaceTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmployeeId { get; set; }
    public byte[] TemplateData { get; set; } = Array.Empty<byte>();
    public double Quality { get; set; }
    public string Algorithm { get; set; } = "Histogram";
    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class OpenCvFaceQualityResult
{
    public double OverallScore { get; set; }
    public double Brightness { get; set; }
    public double Contrast { get; set; }
    public double Sharpness { get; set; }
    public double FaceSize { get; set; }
    public bool IsAcceptable { get; set; }
    public List<string> Issues { get; set; } = new();
}

public class OpenCvFaceComparisonResult
{
    public bool IsMatch { get; set; }
    public double Similarity { get; set; }
    public double Distance { get; set; }
    public double Threshold { get; set; } = 80.0;
    public string Method { get; set; } = "Histogram";
    public DateTime ComparedAt { get; set; } = DateTime.UtcNow;
}