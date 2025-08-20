namespace FaceGuardPro.Shared.Constants;

public static class FaceDetectionConstants
{
    public const double MIN_FACE_DETECTION_CONFIDENCE = 0.7;
    public const double MIN_FACE_QUALITY_SCORE = 0.6;
    public const int MIN_FACE_SIZE_PIXELS = 80;
    public const int MAX_FACE_SIZE_PIXELS = 800;
    public const double EAR_THRESHOLD = 0.25; // Eye Aspect Ratio threshold for blink detection
    public const int BLINK_CONSECUTIVE_FRAMES = 3;
    public const int MAX_FACES_ALLOWED = 1;
}

public static class LivenessConstants
{
    public const double MIN_LIVENESS_CONFIDENCE = 0.8;
    public const double BLINK_SCORE_THRESHOLD = 0.7;
    public const double TEXTURE_SCORE_THRESHOLD = 0.6;
    public const double DEPTH_SCORE_THRESHOLD = 0.5;
    public const double MOTION_SCORE_THRESHOLD = 0.6;
    public const int CHALLENGE_TIMEOUT_SECONDS = 30;
    public const int MAX_CHALLENGE_ATTEMPTS = 3;
    public const double HEAD_MOVEMENT_THRESHOLD = 15.0; // degrees
}

public static class AuthenticationConstants
{
    public const double FACE_MATCH_THRESHOLD = 0.85;
    public const int MAX_AUTHENTICATION_ATTEMPTS = 3;
    public const int LOCKOUT_DURATION_MINUTES = 15;
    public const int TOKEN_EXPIRY_MINUTES = 60;
    public const int REFRESH_TOKEN_EXPIRY_DAYS = 7;

    public static class Roles
    {
        public const string ADMIN = "Admin";
        public const string OPERATOR = "Operator";
        public const string VIEWER = "Viewer";
    }

    public static class Permissions
    {
        public const string MANAGE_EMPLOYEES = "ManageEmployees";
        public const string VIEW_EMPLOYEES = "ViewEmployees";
        public const string MANAGE_FACE_TEMPLATES = "ManageFaceTemplates";
        public const string PERFORM_AUTHENTICATION = "PerformAuthentication";
        public const string VIEW_REPORTS = "ViewReports";
        public const string MANAGE_SYSTEM = "ManageSystem";
    }
}

public static class ImageProcessingConstants
{
    public const int MAX_IMAGE_WIDTH = 1920;
    public const int MAX_IMAGE_HEIGHT = 1080;
    public const int TARGET_FACE_SIZE = 200;
    public const int MIN_IMAGE_WIDTH = 320;
    public const int MIN_IMAGE_HEIGHT = 240;
    public const double JPEG_QUALITY = 0.9;
    public const int MAX_FILE_SIZE_MB = 10;

    public static class SupportedFormats
    {
        public const string JPEG = "image/jpeg";
        public const string PNG = "image/png";
        public const string BMP = "image/bmp";
        public static readonly string[] AllFormats = { JPEG, PNG, BMP };
    }
}

public static class DatabaseConstants
{
    public const int DEFAULT_PAGE_SIZE = 20;
    public const int MAX_PAGE_SIZE = 100;
    public const int CONNECTION_TIMEOUT_SECONDS = 30;
    public const int COMMAND_TIMEOUT_SECONDS = 60;
}

public static class CacheConstants
{
    public const int FACE_TEMPLATE_CACHE_MINUTES = 30;
    public const int EMPLOYEE_CACHE_MINUTES = 15;
    public const int AUTHENTICATION_RESULT_CACHE_MINUTES = 5;
}

public static class LoggingConstants
{
    public static class Categories
    {
        public const string FACE_DETECTION = "FaceDetection";
        public const string LIVENESS_DETECTION = "LivenessDetection";
        public const string AUTHENTICATION = "Authentication";
        public const string EMPLOYEE_MANAGEMENT = "EmployeeManagement";
        public const string SYSTEM = "System";
        public const string SECURITY = "Security";
    }

    public static class Events
    {
        public const int EMPLOYEE_CREATED = 1001;
        public const int EMPLOYEE_UPDATED = 1002;
        public const int EMPLOYEE_DELETED = 1003;
        public const int FACE_TEMPLATE_CREATED = 2001;
        public const int FACE_TEMPLATE_UPDATED = 2002;
        public const int AUTHENTICATION_SUCCESS = 3001;
        public const int AUTHENTICATION_FAILED = 3002;
        public const int LIVENESS_CHECK_PASSED = 4001;
        public const int LIVENESS_CHECK_FAILED = 4002;
        public const int SECURITY_VIOLATION = 5001;
        public const int SYSTEM_ERROR = 6001;
    }
}

public static class ApiConstants
{
    public const string API_VERSION = "v1";
    public const string CONTENT_TYPE_JSON = "application/json";
    public const string CONTENT_TYPE_MULTIPART = "multipart/form-data";

    public static class Routes
    {
        public const string BASE_API = "/api/v1";
        public const string EMPLOYEES = BASE_API + "/employees";
        public const string AUTHENTICATION = BASE_API + "/auth";
        public const string FACE_DETECTION = BASE_API + "/face-detection";
        public const string LIVENESS = BASE_API + "/liveness";
        public const string HEALTH = "/health";
    }

    public static class Headers
    {
        public const string AUTHORIZATION = "Authorization";
        public const string CONTENT_TYPE = "Content-Type";
        public const string ACCEPT = "Accept";
        public const string USER_AGENT = "User-Agent";
        public const string REQUEST_ID = "X-Request-ID";
    }
}

public static class ValidationConstants
{
    public const int MIN_PASSWORD_LENGTH = 6;
    public const int MAX_PASSWORD_LENGTH = 128;
    public const int MIN_USERNAME_LENGTH = 3;
    public const int MAX_USERNAME_LENGTH = 50;
    public const string EMAIL_REGEX = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
    public const string PHONE_REGEX = @"^[\+]?[1-9][\d]{0,15}$";
    public const string EMPLOYEE_ID_REGEX = @"^[A-Z0-9]{3,10}$";
}