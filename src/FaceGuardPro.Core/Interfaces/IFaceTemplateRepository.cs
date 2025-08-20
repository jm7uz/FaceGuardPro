using FaceGuardPro.Data.Entities;

namespace FaceGuardPro.Core.Interfaces;

/// <summary>
/// Repository interface for face template operations
/// </summary>
public interface IFaceTemplateRepository
{
    /// <summary>
    /// Create a new face template
    /// </summary>
    Task<FaceTemplate> CreateAsync(FaceTemplate template);

    /// <summary>
    /// Get face template by ID
    /// </summary>
    Task<FaceTemplate?> GetByIdAsync(string id);

    /// <summary>
    /// Get all face templates for an employee
    /// </summary>
    Task<List<FaceTemplate>> GetByEmployeeIdAsync(string employeeId);

    /// <summary>
    /// Get all face templates
    /// </summary>
    Task<List<FaceTemplate>> GetAllAsync();

    /// <summary>
    /// Get all active face templates
    /// </summary>
    Task<List<FaceTemplate>> GetAllActiveAsync();

    /// <summary>
    /// Update face template
    /// </summary>
    Task<FaceTemplate> UpdateAsync(FaceTemplate template);

    /// <summary>
    /// Delete face template
    /// </summary>
    Task<bool> DeleteAsync(string id);

    /// <summary>
    /// Delete all templates for an employee
    /// </summary>
    Task<bool> DeleteByEmployeeIdAsync(string employeeId);

    /// <summary>
    /// Check if template exists
    /// </summary>
    Task<bool> ExistsAsync(string id);

    /// <summary>
    /// Get templates with quality above threshold
    /// </summary>
    Task<List<FaceTemplate>> GetHighQualityTemplatesAsync(double qualityThreshold);

    /// <summary>
    /// Get template count by employee
    /// </summary>
    Task<int> GetTemplateCountByEmployeeAsync(string employeeId);

    /// <summary>
    /// Get templates created within date range
    /// </summary>
    Task<List<FaceTemplate>> GetTemplatesByDateRangeAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Deactivate template
    /// </summary>
    Task<bool> DeactivateAsync(string id);

    /// <summary>
    /// Activate template
    /// </summary>
    Task<bool> ActivateAsync(string id);

    /// <summary>
    /// Get template statistics
    /// </summary>
    Task<Dictionary<string, object>> GetStatisticsAsync();
}