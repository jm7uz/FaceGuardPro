// src/FaceGuardPro.AI/Configuration/OpenCvConfiguration.cs
namespace FaceGuardPro.AI.Configuration;

public class OpenCvConfiguration
{
    public FaceDetectionConfig FaceDetection { get; set; } = new();
    public FaceRecognitionConfig FaceRecognition { get; set; } = new();
    public ImageProcessingConfig ImageProcessing { get; set; } = new();
    public QualityConfig Quality { get; set; } = new();
}

public class FaceDetectionConfig
{
    public string CascadeFilePath { get; set; } = "Models/haarcascade_frontalface_alt2.xml";
    public double ScaleFactor { get; set; } = 1.1;
    public int MinNeighbors { get; set; } = 4;
    public int MinFaceSize { get; set; } = 80;
    public int MaxFaceSize { get; set; } = 400;
    public double ConfidenceThreshold { get; set; } = 0.7;
}

public class FaceRecognitionConfig
{
    public string Algorithm { get; set; } = "LBPH";
    public int LBPHRadius { get; set; } = 1;
    public int LBPHNeighbors { get; set; } = 8;
    public int LBPHGridX { get; set; } = 8;
    public int LBPHGridY { get; set; } = 8;
    public double RecognitionThreshold { get; set; } = 80.0;
    public double MatchThreshold { get; set; } = 0.85;
}

public class ImageProcessingConfig
{
    public int TargetWidth { get; set; } = 200;
    public int TargetHeight { get; set; } = 200;
    public bool EnableGaussianBlur { get; set; } = true;
    public int BlurKernelSize { get; set; } = 3;
    public bool EnableHistogramEqualization { get; set; } = true;
    public bool EnableNoiseReduction { get; set; } = true;
}

public class QualityConfig
{
    public double MinQualityScore { get; set; } = 0.6;
    public double MinBrightness { get; set; } = 50.0;
    public double MaxBrightness { get; set; } = 200.0;
    public double MinContrast { get; set; } = 0.3;
    public double MinSharpness { get; set; } = 0.4;
    public int MinFacePixels { get; set; } = 6400; // 80x80
}