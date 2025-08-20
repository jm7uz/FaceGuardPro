using FaceGuardPro.AI.DTOs;
using System.Drawing;

namespace FaceGuardPro.AI.Interfaces
{
    /// <summary>
    /// Face recognition service interface
    /// </summary>
    public interface IFaceRecognitionService
    {
        /// <summary>
        /// Face template generate qiladi
        /// </summary>
        /// <param name="imageData">Face image bytes</param>
        /// <param name="faceRegion">Face bounding box (optional)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Face template result</returns>
        Task<FaceTemplateResult> GenerateTemplateAsync(
            byte[] imageData,
            Rectangle? faceRegion = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Ikkita face template'ni compare qiladi
        /// </summary>
        /// <param name="template1">First template</param>
        /// <param name="template2">Second template</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Comparison result</returns>
        Task<FaceComparisonResult> CompareTemplatesAsync(
            byte[] template1,
            byte[] template2,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Face image'ni template bilan compare qiladi
        /// </summary>
        /// <param name="imageData">Face image bytes</param>
        /// <param name="template">Stored template</param>
        /// <param name="faceRegion">Face region (optional)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Comparison result</returns>
        Task<FaceComparisonResult> CompareWithTemplateAsync(
            byte[] imageData,
            byte[] template,
            Rectangle? faceRegion = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Multiple template'lar bilan compare qiladi (1:N matching)
        /// </summary>
        /// <param name="imageData">Query face image</param>
        /// <param name="templates">Stored templates with IDs</param>
        /// <param name="threshold">Similarity threshold</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Best matches</returns>
        Task<List<FaceMatchResult>> CompareWithMultipleTemplatesAsync(
            byte[] imageData,
            Dictionary<string, byte[]> templates,
            double threshold = 80.0,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Face template'ni verify qiladi (template valid mi?)
        /// </summary>
        /// <param name="templateData">Template bytes</param>
        /// <returns>Template valid</returns>
        bool IsValidTemplate(byte[] templateData);

        /// <summary>
        /// Template size info
        /// </summary>
        /// <param name="templateData">Template bytes</param>
        /// <returns>Template information</returns>
        FaceTemplateInfo GetTemplateInfo(byte[] templateData);

        /// <summary>
        /// Multiple face'lardan eng yaxshi template generate qiladi
        /// </summary>
        /// <param name="faceImages">Face images</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Best template result</returns>
        Task<FaceTemplateResult> GenerateBestTemplateAsync(
            List<byte[]> faceImages,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Service health check
        /// </summary>
        /// <returns>Service healthy</returns>
        Task<bool> HealthCheckAsync();

        /// <summary>
        /// Recognition statistics
        /// </summary>
        /// <returns>Statistics</returns>
        Task<Dictionary<string, object>> GetStatisticsAsync();
    }

    /// <summary>
    /// Face match result for 1:N matching
    /// </summary>
    public class FaceMatchResult
    {
        /// <summary>
        /// Template ID
        /// </summary>
        public string TemplateId { get; set; } = string.Empty;

        /// <summary>
        /// Similarity score (0-100)
        /// </summary>
        public double SimilarityScore { get; set; }

        /// <summary>
        /// Distance measure
        /// </summary>
        public double Distance { get; set; }

        /// <summary>
        /// Confidence score
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// Is match (above threshold)
        /// </summary>
        public bool IsMatch { get; set; }

        /// <summary>
        /// Match rank (1 = best match)
        /// </summary>
        public int Rank { get; set; }

        /// <summary>
        /// Comparison metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Face template information
    /// </summary>
    public class FaceTemplateInfo
    {
        /// <summary>
        /// Template size (bytes)
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// Template version
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Template format (LBPH, etc.)
        /// </summary>
        public string Format { get; set; } = "LBPH";

        /// <summary>
        /// Quality score used during generation
        /// </summary>
        public double QualityScore { get; set; }

        /// <summary>
        /// Is template valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Template metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}