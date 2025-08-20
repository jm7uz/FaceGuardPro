using OpenCvSharp;
using System.Drawing;
using System.Drawing.Imaging;

namespace FaceGuardPro.AI.Utilities
{
    /// <summary>
    /// OpenCV utility functions
    /// </summary>
    public static class OpenCvUtilities
    {
        /// <summary>
        /// Byte array'dan Mat object yaratadi
        /// </summary>
        /// <param name="imageData">Image bytes</param>
        /// <returns>OpenCV Mat</returns>
        public static Mat BytesToMat(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0)
                throw new ArgumentException("Image data is null or empty", nameof(imageData));

            return Mat.FromImageData(imageData, ImreadModes.Color);
        }

        /// <summary>
        /// Mat object'ni byte array'ga convert qiladi
        /// </summary>
        /// <param name="mat">OpenCV Mat</param>
        /// <param name="extension">File extension (default: .jpg)</param>
        /// <returns>Image bytes</returns>
        public static byte[] MatToBytes(Mat mat, string extension = ".jpg")
        {
            if (mat == null || mat.Empty())
                throw new ArgumentException("Mat is null or empty", nameof(mat));

            mat.ImEncode(extension, out var imageData);
            return imageData;
        }

        /// <summary>
        /// Image'ni resize qiladi (aspect ratio saqlagan holda)
        /// </summary>
        /// <param name="source">Source Mat</param>
        /// <param name="maxWidth">Maximum width</param>
        /// <param name="maxHeight">Maximum height</param>
        /// <returns>Resized Mat</returns>
        public static Mat ResizeKeepAspectRatio(Mat source, int maxWidth, int maxHeight)
        {
            if (source == null || source.Empty())
                return source;

            var originalWidth = source.Width;
            var originalHeight = source.Height;

            if (originalWidth <= maxWidth && originalHeight <= maxHeight)
                return source.Clone();

            // Calculate scaling factor
            var scaleX = (double)maxWidth / originalWidth;
            var scaleY = (double)maxHeight / originalHeight;
            var scale = Math.Min(scaleX, scaleY);

            var newWidth = (int)(originalWidth * scale);
            var newHeight = (int)(originalHeight * scale);

            var resized = new Mat();
            Cv2.Resize(source, resized, new OpenCvSharp.Size(newWidth, newHeight), 0, 0, InterpolationFlags.Area);

            return resized;
        }

        /// <summary>
        /// Image quality enhancement
        /// </summary>
        /// <param name="source">Source Mat</param>
        /// <returns>Enhanced Mat</returns>
        public static Mat EnhanceImage(Mat source)
        {
            if (source == null || source.Empty())
                return source;

            var enhanced = source.Clone();

            // Convert to grayscale for processing
            var gray = new Mat();
            if (enhanced.Channels() == 3)
            {
                Cv2.CvtColor(enhanced, gray, ColorConversionCodes.BGR2GRAY);
            }
            else
            {
                gray = enhanced.Clone();
            }

            // Apply histogram equalization
            var equalized = new Mat();
            Cv2.EqualizeHist(gray, equalized);

            // Apply Gaussian blur to reduce noise
            var blurred = new Mat();
            Cv2.GaussianBlur(equalized, blurred, new OpenCvSharp.Size(3, 3), 0);

            // Convert back to color if needed
            if (source.Channels() == 3)
            {
                Cv2.CvtColor(blurred, enhanced, ColorConversionCodes.GRAY2BGR);
            }
            else
            {
                enhanced = blurred;
            }

            gray?.Dispose();
            equalized?.Dispose();
            blurred?.Dispose();

            return enhanced;
        }

        /// <summary>
        /// Image brightness va contrast assessment
        /// </summary>
        /// <param name="source">Source Mat</param>
        /// <returns>Brightness and contrast scores</returns>
        public static (double brightness, double contrast) AssessImageQuality(Mat source)
        {
            if (source == null || source.Empty())
                return (0, 0);

            var gray = new Mat();
            if (source.Channels() == 3)
            {
                Cv2.CvtColor(source, gray, ColorConversionCodes.BGR2GRAY);
            }
            else
            {
                gray = source.Clone();
            }

            // Calculate mean (brightness)
            var mean = Cv2.Mean(gray);
            var brightness = mean.Val0 / 255.0 * 100;

            // Calculate standard deviation (contrast)
            var meanScalar = new Scalar(mean.Val0);
            var stdDev = new Mat();
            var meanMat = new Mat(gray.Size(), gray.Type(), meanScalar);

            Cv2.Subtract(gray, meanMat, stdDev);
            Cv2.Multiply(stdDev, stdDev, stdDev);

            var variance = Cv2.Mean(stdDev).Val0;
            var contrast = Math.Sqrt(variance) / 255.0 * 100;

            gray?.Dispose();
            stdDev?.Dispose();
            meanMat?.Dispose();

            return (brightness, contrast);
        }

        /// <summary>
        /// Sharpness assessment using Laplacian
        /// </summary>
        /// <param name="source">Source Mat</param>
        /// <returns>Sharpness score (0-100)</returns>
        public static double AssessSharpness(Mat source)
        {
            if (source == null || source.Empty())
                return 0;

            var gray = new Mat();
            if (source.Channels() == 3)
            {
                Cv2.CvtColor(source, gray, ColorConversionCodes.BGR2GRAY);
            }
            else
            {
                gray = source.Clone();
            }

            var laplacian = new Mat();
            Cv2.Laplacian(gray, laplacian, MatType.CV_64F);

            var meanScalar = new Scalar();
            var stdScalar = new Scalar();
            Cv2.MeanStdDev(laplacian, out meanScalar, out stdScalar);

            var sharpness = stdScalar.Val0 * stdScalar.Val0;
            var normalizedSharpness = Math.Min(sharpness / 1000.0 * 100, 100);

            gray?.Dispose();
            laplacian?.Dispose();

            return normalizedSharpness;
        }

        /// <summary>
        /// Face region'ni crop qiladi
        /// </summary>
        /// <param name="source">Source Mat</param>
        /// <param name="faceRect">Face rectangle</param>
        /// <param name="padding">Padding percentage (0.1 = 10%)</param>
        /// <returns>Cropped face Mat</returns>
        public static Mat CropFace(Mat source, Rect faceRect, double padding = 0.1)
        {
            if (source == null || source.Empty())
                throw new ArgumentException("Source mat is null or empty");

            // Add padding
            var padX = (int)(faceRect.Width * padding);
            var padY = (int)(faceRect.Height * padding);

            var x = Math.Max(0, faceRect.X - padX);
            var y = Math.Max(0, faceRect.Y - padY);
            var width = Math.Min(source.Width - x, faceRect.Width + 2 * padX);
            var height = Math.Min(source.Height - y, faceRect.Height + 2 * padY);

            var cropRect = new Rect(x, y, width, height);
            return source[cropRect].Clone();
        }

        /// <summary>
        /// OpenCV Rect'ni System.Drawing.Rectangle'ga convert qiladi
        /// </summary>
        /// <param name="rect">OpenCV Rect</param>
        /// <returns>System.Drawing.Rectangle</returns>
        public static Rectangle RectToRectangle(Rect rect)
        {
            return new Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
        }

        /// <summary>
        /// System.Drawing.Rectangle'ni OpenCV Rect'ga convert qiladi
        /// </summary>
        /// <param name="rectangle">System.Drawing.Rectangle</param>
        /// <returns>OpenCV Rect</returns>
        public static Rect RectangleToRect(Rectangle rectangle)
        {
            return new Rect(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
        }

        /// <summary>
        /// Image format validation
        /// </summary>
        /// <param name="imageData">Image bytes</param>
        /// <returns>Valid image format</returns>
        public static bool IsValidImageFormat(byte[] imageData)
        {
            if (imageData == null || imageData.Length < 4)
                return false;

            try
            {
                using var mat = BytesToMat(imageData);
                return !mat.Empty();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Image format detection
        /// </summary>
        /// <param name="imageData">Image bytes</param>
        /// <returns>Image format</returns>
        public static string DetectImageFormat(byte[] imageData)
        {
            if (imageData == null || imageData.Length < 4)
                return "unknown";

            // Check PNG signature
            if (imageData[0] == 0x89 && imageData[1] == 0x50 && imageData[2] == 0x4E && imageData[3] == 0x47)
                return "png";

            // Check JPEG signature
            if (imageData[0] == 0xFF && imageData[1] == 0xD8)
                return "jpg";

            // Check BMP signature
            if (imageData[0] == 0x42 && imageData[1] == 0x4D)
                return "bmp";

            // Check GIF signature
            if (imageData[0] == 0x47 && imageData[1] == 0x49 && imageData[2] == 0x46)
                return "gif";

            return "unknown";
        }

        /// <summary>
        /// Mat disposal helper
        /// </summary>
        /// <param name="mats">Mat objects to dispose</param>
        public static void DisposeMats(params Mat?[] mats)
        {
            foreach (var mat in mats)
            {
                mat?.Dispose();
            }
        }

        /// <summary>
        /// Debug image save helper
        /// </summary>
        /// <param name="mat">Mat to save</param>
        /// <param name="fileName">File name</param>
        /// <param name="debugFolder">Debug folder path</param>
        public static void SaveDebugImage(Mat mat, string fileName, string debugFolder)
        {
            if (mat == null || mat.Empty()) return;

            try
            {
                if (!Directory.Exists(debugFolder))
                {
                    Directory.CreateDirectory(debugFolder);
                }

                var filePath = Path.Combine(debugFolder, fileName);
                mat.SaveImage(filePath);
            }
            catch
            {
                // Debug save qolasa ham davom etamiz
            }
        }
    }
}