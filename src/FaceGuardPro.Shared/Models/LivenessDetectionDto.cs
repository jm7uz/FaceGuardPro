using FaceGuardPro.Shared.Enums;

namespace FaceGuardPro.Shared.Models;

public class LivenessDetectionDto
{
    public LivenessResult Result { get; set; }
    public string Message { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public LivenessMetrics? Metrics { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan ProcessingTime { get; set; }
}

public class LivenessMetrics
{
    public double BlinkScore { get; set; }
    public double TextureScore { get; set; }
    public double DepthScore { get; set; }
    public double MotionScore { get; set; }
    public double OverallScore { get; set; }
    public bool BlinkDetected { get; set; }
    public bool HeadMovementDetected { get; set; }
    public bool TextureAnalysisPassed { get; set; }

    public List<LivenessCheck> Checks { get; set; } = new();
}

public class LivenessCheck
{
    public string CheckType { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public double Score { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class ChallengeDto
{
    public Guid Id { get; set; }
    public ChallengeType Type { get; set; }
    public ChallengeStatus Status { get; set; }
    public string Instruction { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public double? Score { get; set; }
    public string? ResultMessage { get; set; }
}

public class CreateChallengeDto
{
    public ChallengeType Type { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
    public string? CustomInstruction { get; set; }
}

public class ChallengeResponseDto
{
    public Guid ChallengeId { get; set; }
    public byte[] ResponseImageData { get; set; } = Array.Empty<byte>();
    public DateTime ResponseTime { get; set; } = DateTime.UtcNow;
}

public class BlinkDetectionDto
{
    public bool BlinkDetected { get; set; }
    public double LeftEyeAspectRatio { get; set; }
    public double RightEyeAspectRatio { get; set; }
    public double AverageEAR { get; set; }
    public double BlinkThreshold { get; set; } = 0.25;
    public List<Point2D> LeftEyePoints { get; set; } = new();
    public List<Point2D> RightEyePoints { get; set; } = new();
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
}

public class HeadPoseDto
{
    public double Yaw { get; set; }   // Head rotation left-right
    public double Pitch { get; set; } // Head rotation up-down
    public double Roll { get; set; }  // Head tilt
    public Vector3D RotationVector { get; set; } = new();
    public Vector3D TranslationVector { get; set; } = new();
    public double Confidence { get; set; }
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
}

public class Vector3D
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }

    public Vector3D() { }

    public Vector3D(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public double Magnitude => Math.Sqrt(X * X + Y * Y + Z * Z);
}

public class LivenessSessionDto
{
    public Guid Id { get; set; }
    public List<ChallengeDto> Challenges { get; set; } = new();
    public ChallengeStatus OverallStatus { get; set; }
    public double OverallScore { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? Duration { get; set; }
    public string? FailureReason { get; set; }
}