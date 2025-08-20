using FaceGuardPro.Shared.Enums;

namespace FaceGuardPro.Shared.Models;

public class FaceDetectionDto
{
    public FaceDetectionResult Result { get; set; }
    public string Message { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public BoundingBox? FaceBox { get; set; }
    public List<FaceLandmark> Landmarks { get; set; } = new();
    public FaceQualityMetrics? QualityMetrics { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

public class BoundingBox
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public int CenterX => X + Width / 2;
    public int CenterY => Y + Height / 2;
    public int Area => Width * Height;
}

public class FaceLandmark
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Point2D Point { get; set; } = new();
    public double Confidence { get; set; }
}

public class Point2D
{
    public float X { get; set; }
    public float Y { get; set; }

    public Point2D() { }

    public Point2D(float x, float y)
    {
        X = x;
        Y = y;
    }
}

public class FaceQualityMetrics
{
    public double OverallQuality { get; set; }
    public double Brightness { get; set; }
    public double Contrast { get; set; }
    public double Sharpness { get; set; }
    public double Symmetry { get; set; }
    public double FaceSize { get; set; }
    public double EyeDistance { get; set; }
    public bool IsBlurry { get; set; }
    public bool IsTooLightingPoor { get; set; }
    public bool IsFaceTooSmall { get; set; }
    public bool IsFaceTooLarge { get; set; }

    public bool IsQualityAcceptable => OverallQuality >= 0.7 &&
                                     !IsBlurry &&
                                     !IsTooLightingPoor &&
                                     !IsFaceTooSmall &&
                                     !IsFaceTooLarge;
}

public class FaceTemplateDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public byte[] TemplateData { get; set; } = Array.Empty<byte>();
    public double Quality { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties for display
    public string? EmployeeName { get; set; }
    public string? EmployeeId_Display { get; set; }
}

public class CreateFaceTemplateDto
{
    public Guid EmployeeId { get; set; }
    public byte[] ImageData { get; set; } = Array.Empty<byte>();
}

public class FaceComparisonResult
{
    public bool IsMatch { get; set; }
    public double Similarity { get; set; }
    public double Threshold { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime ComparedAt { get; set; } = DateTime.UtcNow;
}