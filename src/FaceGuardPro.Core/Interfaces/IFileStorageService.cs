using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FaceGuardPro.Core.Interfaces;

public interface IFileStorageService
{
    // Employee Photo Operations
    Task<string> SaveEmployeePhotoAsync(Guid employeeId, byte[] photoData, string fileName);
    Task<byte[]?> GetEmployeePhotoAsync(Guid employeeId);
    Task<bool> DeleteEmployeePhotoAsync(Guid employeeId);

    // General File Operations
    Task<string> SaveFileAsync(byte[] fileData, string fileName, string folder);
    Task<byte[]?> GetFileAsync(string filePath);
    Task<bool> DeleteFileAsync(string filePath);
    Task<bool> FileExistsAsync(string filePath);

    // Image Validation
    Task<ImageValidationResult> ValidateImageAsync(byte[] imageData);
    Task<byte[]> ResizeImageAsync(byte[] imageData, int maxWidth, int maxHeight);
    Task<byte[]> CompressImageAsync(byte[] imageData, double quality = 0.8);

    // Directory Operations
    Task<bool> CreateDirectoryAsync(string directoryPath);
    Task<bool> DeleteDirectoryAsync(string directoryPath);
    Task<IEnumerable<string>> GetFilesInDirectoryAsync(string directoryPath);

    // File Info
    Task<long> GetFileSizeAsync(string filePath);
    Task<DateTime> GetFileCreatedDateAsync(string filePath);
    Task<DateTime> GetFileModifiedDateAsync(string filePath);
}

public class ImageValidationResult
{
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string Format { get; set; } = string.Empty;
}