using System.Drawing;

namespace FaceGuardPro.AI.DTOs
{
    /// <summary>
    /// Face detection result
    /// </summary>
    public class FaceDetectionResult
    {
        /// <summary>
        /// Face topilganmi
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Topilgan face'lar soni
        /// </summary>
        public int FaceCount { get; set; }

        /// <summary>
        /// Topilgan face'lar
        /// </summary>
        public List<DetectedFace> Faces { get; set; } = new();

        /// <summary>
        /// Processing time (milliseconds)
        /// </summary>
        public long ProcessingTimeMs { get; set; }

        /// <summary>
        /// Error message (agar bo'lsa)
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Original image dimensions
        /// </summary>
        public Size ImageSize { get; set; }
    }

    /// <summary>
    /// Detected face information
    /// </summary>
    public class DetectedFace
    {
        /// <summary>
        /// Face ID (unique identifier)
        /// </summary>
        public Guid FaceId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Face bounding box
        /// </summary>
        public Rectangle BoundingBox { get; set; }

        /// <summary>
        /// Face quality score (0-100)
        /// </summary>
        public double QualityScore { get; set; }

        /// <summary>
        /// Face confidence score (0-100)
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// Face landmarks (68 points)
        /// </summary>
        public List<Point> Landmarks { get; set; } = new();

        /// <summary>
        /// Face template (encoded face features)
        /// </summary>
        public byte[]? Template { get; set; }

        /// <summary>
        /// Face angle (rotation)
        /// </summary>
        public double Angle { get; set; }

        /// <summary>
        /// Face size category
        /// </summary>
        public FaceSizeCategory SizeCategory { get; set; }

        /// <summary>
        /// Additional metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Face comparison result
    /// </summary>
    public class FaceComparisonResult
    {
        /// <summary>
        /// Comparison successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Similarity score (0-100)
        /// </summary>
        public double SimilarityScore { get; set; }

        /// <summary>
        /// Is match (based on threshold)
        /// </summary>
        public bool IsMatch { get; set; }

        /// <summary>
        /// Confidence score (0-100)
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// Distance measure
        /// </summary>
        public double Distance { get; set; }

        /// <summary>
        /// Processing time
        /// </summary>
        public long ProcessingTimeMs { get; set; }

        /// <summary>
        /// Error message
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Comparison metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Face template generation result
    /// </summary>
    public class FaceTemplateResult
    {
        /// <summary>
        /// Template generation successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Generated template data
        /// </summary>
        public byte[]? TemplateData { get; set; }

        /// <summary>
        /// Template version
        /// </summary>
        public string Version { get; set; } = "1.0";

        /// <summary>
        /// Template quality score
        /// </summary>
        public double QualityScore { get; set; }

        /// <summary>
        /// Template size (bytes)
        /// </summary>
        public int TemplateSize { get; set; }

        /// <summary>
        /// Source face information
        /// </summary>
        public DetectedFace? SourceFace { get; set; }

        /// <summary>
        /// Processing time
        /// </summary>
        public long ProcessingTimeMs { get; set; }

        /// <summary>
        /// Error message
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Face processing request
    /// </summary>
    public class FaceProcessingRequest
    {
        /// <summary>
        /// Image data
        /// </summary>
        public byte[] ImageData { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Image format (jpg, png, etc.)
        /// </summary>
        public string ImageFormat { get; set; } = "jpg";

        /// <summary>
        /// Employee ID (agar ma'lum bo'lsa)
        /// </summary>
        public string? EmployeeId { get; set; }

        /// <summary>
        /// Processing options
        /// </summary>
        public FaceProcessingOptions Options { get; set; } = new();
    }

    /// <summary>
    /// Face processing options
    /// </summary>
    public class FaceProcessingOptions
    {
        /// <summary>
        /// Detect multiple faces
        /// </summary>
        public bool DetectMultipleFaces { get; set; } = false;

        /// <summary>
        /// Generate face template
        /// </summary>
        public bool GenerateTemplate { get; set; } = true;

        /// <summary>
        /// Calculate quality score
        /// </summary>
        public bool CalculateQuality { get; set; } = true;

        /// <summary>
        /// Detect landmarks
        /// </summary>
        public bool DetectLandmarks { get; set; } = false;

        /// <summary>
        /// Resize image before processing
        /// </summary>
        public bool ResizeImage { get; set; } = true;

        /// <summary>
        /// Apply histogram equalization
        /// </summary>
        public bool ApplyHistogramEqualization { get; set; } = true;

        /// <summary>
        /// Save debug images
        /// </summary>
        public bool SaveDebugImages { get; set; } = false;
    }

    /// <summary>
    /// Face size categories
    /// </summary>
    public enum FaceSizeCategory
    {
        VerySmall,
        Small,
        Medium,
        Large,
        VeryLarge
    }

    /// <summary>
    /// Face quality assessment
    /// </summary>
    public class FaceQualityAssessment
    {
        /// <summary>
        /// Overall quality score (0-100)
        /// </summary>
        public double OverallScore { get; set; }

        /// <summary>
        /// Brightness score (0-100)
        /// </summary>
        public double BrightnessScore { get; set; }

        /// <summary>
        /// Contrast score (0-100)
        /// </summary>
        public double ContrastScore { get; set; }

        /// <summary>
        /// Sharpness score (0-100)
        /// </summary>
        public double SharpnessScore { get; set; }

        /// <summary>
        /// Face size score (0-100)
        /// </summary>
        public double SizeScore { get; set; }

        /// <summary>
        /// Face angle score (0-100)
        /// </summary>
        public double AngleScore { get; set; }

        /// <summary>
        /// Noise level score (0-100)
        /// </summary>
        public double NoiseScore { get; set; }

        /// <summary>
        /// Quality factors breakdown
        /// </summary>
        public Dictionary<string, double> Factors { get; set; } = new();
    }
}