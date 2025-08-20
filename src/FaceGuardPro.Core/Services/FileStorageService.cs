using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using FaceGuardPro.Core.Interfaces;
using System.Drawing;
using System.Drawing.Imaging;

namespace FaceGuardPro.Core.Services;

public class FileStorageService : IFileStorageService
{
    private readonly string _basePath;
    private readonly ILogger<FileStorageService> _logger;
    private const long MAX_FILE_SIZE = 10 * 1024 * 1024; // 10MB
    private static readonly string[] ALLOWED_EXTENSIONS = { ".jpg", ".jpeg", ".png", ".bmp" };

    public FileStorageService(IConfiguration configuration, ILogger<FileStorageService> logger)
    {
        _basePath = configuration["FileStorage:BasePath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "Storage");
        _logger = logger;

        // Ensure base directory exists
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> SaveEmployeePhotoAsync(Guid employeeId, byte[] photoData, string fileName)
    {
        try
        {
            var employeeFolder = Path.Combine(_basePath, "Employees", employeeId.ToString());
            await CreateDirectoryAsync(employeeFolder);

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var newFileName = $"photo_{DateTime.UtcNow:yyyyMMdd_HHmmss}{extension}";
            var filePath = Path.Combine(employeeFolder, newFileName);

            await File.WriteAllBytesAsync(filePath, photoData);

            var relativePath = Path.GetRelativePath(_basePath, filePath);
            _logger.LogInformation("Employee photo saved: {FilePath}", relativePath);

            return relativePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving employee photo for ID: {EmployeeId}", employeeId);
            throw;
        }
    }

    public async Task<byte[]?> GetEmployeePhotoAsync(Guid employeeId)
    {
        try
        {
            var employeeFolder = Path.Combine(_basePath, "Employees", employeeId.ToString());
            if (!Directory.Exists(employeeFolder))
                return null;

            var photoFiles = Directory.GetFiles(employeeFolder, "photo_*.*")
                .Where(f => ALLOWED_EXTENSIONS.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .OrderByDescending(f => File.GetCreationTime(f))
                .FirstOrDefault();

            if (photoFiles == null)
                return null;

            return await File.ReadAllBytesAsync(photoFiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employee photo for ID: {EmployeeId}", employeeId);
            return null;
        }
    }

    public async Task<bool> DeleteEmployeePhotoAsync(Guid employeeId)
    {
        try
        {
            var employeeFolder = Path.Combine(_basePath, "Employees", employeeId.ToString());
            if (!Directory.Exists(employeeFolder))
                return true;

            var photoFiles = Directory.GetFiles(employeeFolder, "photo_*.*");
            foreach (var file in photoFiles)
            {
                File.Delete(file);
            }

            _logger.LogInformation("Employee photos deleted for ID: {EmployeeId}", employeeId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting employee photo for ID: {EmployeeId}", employeeId);
            return false;
        }
    }

    public async Task<string> SaveFileAsync(byte[] fileData, string fileName, string folder)
    {
        try
        {
            var folderPath = Path.Combine(_basePath, folder);
            await CreateDirectoryAsync(folderPath);

            var filePath = Path.Combine(folderPath, fileName);
            await File.WriteAllBytesAsync(filePath, fileData);

            var relativePath = Path.GetRelativePath(_basePath, filePath);
            _logger.LogInformation("File saved: {FilePath}", relativePath);

            return relativePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving file: {FileName}", fileName);
            throw;
        }
    }

    public async Task<byte[]?> GetFileAsync(string filePath)
    {
        try
        {
            var fullPath = Path.Combine(_basePath, filePath);
            if (!File.Exists(fullPath))
                return null;

            return await File.ReadAllBytesAsync(fullPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving file: {FilePath}", filePath);
            return null;
        }
    }

    public async Task<bool> DeleteFileAsync(string filePath)
    {
        try
        {
            var fullPath = Path.Combine(_basePath, filePath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("File deleted: {FilePath}", filePath);
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FilePath}", filePath);
            return false;
        }
    }

    public async Task<bool> FileExistsAsync(string filePath)
    {
        try
        {
            var fullPath = Path.Combine(_basePath, filePath);
            return File.Exists(fullPath);
        }
        catch
        {
            return false;
        }
    }

    public async Task<ImageValidationResult> ValidateImageAsync(byte[] imageData)
    {
        var result = new ImageValidationResult();

        try
        {
            if (imageData == null || imageData.Length == 0)
            {
                result.ErrorMessage = "Image data is empty";
                return result;
            }

            result.FileSize = imageData.Length;

            if (result.FileSize > MAX_FILE_SIZE)
            {
                result.ErrorMessage = $"File size exceeds maximum limit of {MAX_FILE_SIZE / (1024 * 1024)}MB";
                return result;
            }

            // Basic image format validation
            using var ms = new MemoryStream(imageData);
            try
            {
                using var image = Image.FromStream(ms);
                result.Width = image.Width;
                result.Height = image.Height;
                result.Format = image.RawFormat.ToString();
                result.IsValid = true;
            }
            catch
            {
                result.ErrorMessage = "Invalid image format";
                return result;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating image");
            result.ErrorMessage = "Error validating image";
            return result;
        }
    }

    public async Task<byte[]> ResizeImageAsync(byte[] imageData, int maxWidth, int maxHeight)
    {
        try
        {
            using var inputStream = new MemoryStream(imageData);
            using var image = Image.FromStream(inputStream);

            var ratioX = (double)maxWidth / image.Width;
            var ratioY = (double)maxHeight / image.Height;
            var ratio = Math.Min(ratioX, ratioY);

            var newWidth = (int)(image.Width * ratio);
            var newHeight = (int)(image.Height * ratio);

            using var newImage = new Bitmap(newWidth, newHeight);
            using var graphics = Graphics.FromImage(newImage);

            graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

            graphics.DrawImage(image, 0, 0, newWidth, newHeight);

            using var outputStream = new MemoryStream();
            newImage.Save(outputStream, ImageFormat.Jpeg);
            return outputStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resizing image");
            throw;
        }
    }

    public async Task<byte[]> CompressImageAsync(byte[] imageData, double quality = 0.8)
    {
        try
        {
            using var inputStream = new MemoryStream(imageData);
            using var image = Image.FromStream(inputStream);

            var encoder = ImageCodecInfo.GetImageEncoders()
                .FirstOrDefault(e => e.FormatID == ImageFormat.Jpeg.Guid);

            var encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, (long)(quality * 100));

            using var outputStream = new MemoryStream();
            image.Save(outputStream, encoder, encoderParams);
            return outputStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error compressing image");
            throw;
        }
    }

    public async Task<bool> CreateDirectoryAsync(string directoryPath)
    {
        try
        {
            Directory.CreateDirectory(directoryPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating directory: {DirectoryPath}", directoryPath);
            return false;
        }
    }

    public async Task<bool> DeleteDirectoryAsync(string directoryPath)
    {
        try
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, true);
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting directory: {DirectoryPath}", directoryPath);
            return false;
        }
    }

    public async Task<IEnumerable<string>> GetFilesInDirectoryAsync(string directoryPath)
    {
        try
        {
            var fullPath = Path.Combine(_basePath, directoryPath);
            if (!Directory.Exists(fullPath))
                return Enumerable.Empty<string>();

            return Directory.GetFiles(fullPath).Select(f => Path.GetRelativePath(_basePath, f));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting files in directory: {DirectoryPath}", directoryPath);
            return Enumerable.Empty<string>();
        }
    }

    public async Task<long> GetFileSizeAsync(string filePath)
    {
        try
        {
            var fullPath = Path.Combine(_basePath, filePath);
            return new FileInfo(fullPath).Length;
        }
        catch
        {
            return 0;
        }
    }

    public async Task<DateTime> GetFileCreatedDateAsync(string filePath)
    {
        try
        {
            var fullPath = Path.Combine(_basePath, filePath);
            return File.GetCreationTime(fullPath);
        }
        catch
        {
            return DateTime.MinValue;
        }
    }

    public async Task<DateTime> GetFileModifiedDateAsync(string filePath)
    {
        try
        {
            var fullPath = Path.Combine(_basePath, filePath);
            return File.GetLastWriteTime(fullPath);
        }
        catch
        {
            return DateTime.MinValue;
        }
    }
}