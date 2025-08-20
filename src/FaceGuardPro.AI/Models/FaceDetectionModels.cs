using System.Reflection;

namespace FaceGuardPro.AI.Models
{
    /// <summary>
    /// Face detection uchun Haar Cascade modellarini manage qiladi
    /// </summary>
    public static class FaceDetectionModels
    {
        private static readonly Dictionary<string, string> _modelPaths = new();
        private static bool _initialized = false;
        private static readonly object _lock = new();

        /// <summary>
        /// Modellarni initialize qiladi
        /// </summary>
        public static void Initialize()
        {
            lock (_lock)
            {
                if (_initialized) return;

                var assembly = Assembly.GetExecutingAssembly();
                var tempDir = Path.Combine(Path.GetTempPath(), "FaceGuardPro", "Models");

                if (!Directory.Exists(tempDir))
                {
                    Directory.CreateDirectory(tempDir);
                }

                // Embedded resource'lardan modellarni extract qilamiz
                ExtractModel(assembly, "haarcascade_frontalface_default.xml", tempDir);
                ExtractModel(assembly, "haarcascade_frontalface_alt.xml", tempDir);
                ExtractModel(assembly, "haarcascade_eye.xml", tempDir);
                ExtractModel(assembly, "haarcascade_profileface.xml", tempDir);

                _initialized = true;
            }
        }

        private static void ExtractModel(Assembly assembly, string resourceName, string targetDir)
        {
            var fullResourceName = $"FaceGuardPro.AI.Models.{resourceName}";
            var targetPath = Path.Combine(targetDir, resourceName);

            if (File.Exists(targetPath)) return;

            using var stream = assembly.GetManifestResourceStream(fullResourceName);
            if (stream == null)
            {
                throw new FileNotFoundException($"Model resource not found: {fullResourceName}");
            }

            using var fileStream = File.Create(targetPath);
            stream.CopyTo(fileStream);

            _modelPaths[resourceName] = targetPath;
        }

        /// <summary>
        /// Frontal face detection model path
        /// </summary>
        public static string FrontalFaceDefault => GetModelPath("haarcascade_frontalface_default.xml");

        /// <summary>
        /// Alternative frontal face detection model path  
        /// </summary>
        public static string FrontalFaceAlt => GetModelPath("haarcascade_frontalface_alt.xml");

        /// <summary>
        /// Eye detection model path
        /// </summary>
        public static string Eye => GetModelPath("haarcascade_eye.xml");

        /// <summary>
        /// Profile face detection model path
        /// </summary>
        public static string ProfileFace => GetModelPath("haarcascade_profileface.xml");

        private static string GetModelPath(string modelName)
        {
            if (!_initialized)
            {
                Initialize();
            }

            if (_modelPaths.TryGetValue(modelName, out var path))
            {
                return path;
            }

            var tempDir = Path.Combine(Path.GetTempPath(), "FaceGuardPro", "Models");
            return Path.Combine(tempDir, modelName);
        }
    }
}