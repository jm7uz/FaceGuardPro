namespace FaceGuardPro.AI.Configuration
{
    /// <summary>
    /// Face detection va recognition uchun configuration
    /// </summary>
    public class FaceDetectionConfig
    {
        /// <summary>
        /// Minimum face size (pixels)
        /// </summary>
        public int MinFaceSize { get; set; } = 80;

        /// <summary>
        /// Maximum face size (pixels, 0 = unlimited)
        /// </summary>
        public int MaxFaceSize { get; set; } = 0;

        /// <summary>
        /// Scale factor for detection pyramid
        /// </summary>
        public double ScaleFactor { get; set; } = 1.1;

        /// <summary>
        /// Minimum neighbors required for detection
        /// </summary>
        public int MinNeighbors { get; set; } = 5;

        /// <summary>
        /// Image processing uchun maximum width
        /// </summary>
        public int MaxImageWidth { get; set; } = 1024;

        /// <summary>
        /// Image processing uchun maximum height
        /// </summary>
        public int MaxImageHeight { get; set; } = 768;

        /// <summary>
        /// Face quality score threshold (0-100)
        /// </summary>
        public double QualityThreshold { get; set; } = 70.0;

        /// <summary>
        /// Face recognition confidence threshold (0-100)
        /// </summary>
        public double RecognitionThreshold { get; set; } = 80.0;

        /// <summary>
        /// LBPH radius parameter
        /// </summary>
        public int LBPHRadius { get; set; } = 1;

        /// <summary>
        /// LBPH neighbors parameter
        /// </summary>
        public int LBPHNeighbors { get; set; } = 8;

        /// <summary>
        /// LBPH grid X parameter
        /// </summary>
        public int LBPHGridX { get; set; } = 8;

        /// <summary>
        /// LBPH grid Y parameter
        /// </summary>
        public int LBPHGridY { get; set; } = 8;

        /// <summary>
        /// Face template image size
        /// </summary>
        public int TemplateSize { get; set; } = 200;

        /// <summary>
        /// Parallel processing uchun max thread count
        /// </summary>
        public int MaxThreads { get; set; } = Environment.ProcessorCount;

        /// <summary>
        /// Debug mode (save intermediate images)
        /// </summary>
        public bool DebugMode { get; set; } = false;

        /// <summary>
        /// Debug images uchun folder
        /// </summary>
        public string DebugFolder { get; set; } = "Debug/FaceDetection";

        /// <summary>
        /// Processing timeout (seconds)
        /// </summary>
        public int ProcessingTimeout { get; set; } = 30;

        /// <summary>
        /// Default configuration yaratadi
        /// </summary>
        public static FaceDetectionConfig Default => new()
        {
            MinFaceSize = 80,
            MaxFaceSize = 0,
            ScaleFactor = 1.1,
            MinNeighbors = 5,
            MaxImageWidth = 1024,
            MaxImageHeight = 768,
            QualityThreshold = 70.0,
            RecognitionThreshold = 80.0,
            LBPHRadius = 1,
            LBPHNeighbors = 8,
            LBPHGridX = 8,
            LBPHGridY = 8,
            TemplateSize = 200,
            MaxThreads = Environment.ProcessorCount,
            DebugMode = false,
            DebugFolder = "Debug/FaceDetection",
            ProcessingTimeout = 30
        };

        /// <summary>
        /// High accuracy configuration
        /// </summary>
        public static FaceDetectionConfig HighAccuracy => new()
        {
            MinFaceSize = 60,
            MaxFaceSize = 0,
            ScaleFactor = 1.05,
            MinNeighbors = 7,
            MaxImageWidth = 1280,
            MaxImageHeight = 960,
            QualityThreshold = 80.0,
            RecognitionThreshold = 85.0,
            LBPHRadius = 2,
            LBPHNeighbors = 8,
            LBPHGridX = 10,
            LBPHGridY = 10,
            TemplateSize = 250,
            MaxThreads = Environment.ProcessorCount,
            DebugMode = false,
            DebugFolder = "Debug/FaceDetection",
            ProcessingTimeout = 45
        };

        /// <summary>
        /// Fast processing configuration
        /// </summary>
        public static FaceDetectionConfig FastProcessing => new()
        {
            MinFaceSize = 100,
            MaxFaceSize = 400,
            ScaleFactor = 1.2,
            MinNeighbors = 3,
            MaxImageWidth = 640,
            MaxImageHeight = 480,
            QualityThreshold = 60.0,
            RecognitionThreshold = 75.0,
            LBPHRadius = 1,
            LBPHNeighbors = 8,
            LBPHGridX = 6,
            LBPHGridY = 6,
            TemplateSize = 150,
            MaxThreads = Environment.ProcessorCount,
            DebugMode = false,
            DebugFolder = "Debug/FaceDetection",
            ProcessingTimeout = 15
        };
    }
}