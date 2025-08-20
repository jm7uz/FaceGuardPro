namespace FaceGuardPro.Shared.Enums;

public enum EmployeeStatus
{
    Active = 1,
    Inactive = 2,
    Suspended = 3,
    Terminated = 4
}

public enum FaceDetectionResult
{
    Success = 1,
    NoFaceDetected = 2,
    MultipleFacesDetected = 3,
    PoorQuality = 4,
    TooSmall = 5,
    TooLarge = 6,
    BlurryImage = 7,
    BadLighting = 8
}

public enum LivenessResult
{
    Live = 1,
    Spoof = 2,
    Uncertain = 3,
    NoFaceDetected = 4,
    PoorQuality = 5
}

public enum AuthenticationResult
{
    Success = 1,
    EmployeeNotFound = 2,
    NoFaceTemplate = 3,
    FaceNotMatched = 4,
    LivenessCheckFailed = 5,
    PoorImageQuality = 6,
    SystemError = 7
}

public enum ChallengeType
{
    Blink = 1,
    SmileChallenge = 2,
    HeadTurnLeft = 3,
    HeadTurnRight = 4,
    HeadTurnUp = 5,
    HeadTurnDown = 6,
    RandomBlink = 7,
    SequentialActions = 8
}

public enum ChallengeStatus
{
    Pending = 1,
    InProgress = 2,
    Success = 3,
    Failed = 4,
    Timeout = 5,
    Cancelled = 6
}

public enum LogLevel
{
    Debug = 1,
    Information = 2,
    Warning = 3,
    Error = 4,
    Critical = 5
}

public enum ApiResponseStatus
{
    Success = 200,
    Created = 201,
    BadRequest = 400,
    Unauthorized = 401,
    Forbidden = 403,
    NotFound = 404,
    Conflict = 409,
    ValidationError = 422,
    InternalServerError = 500
}