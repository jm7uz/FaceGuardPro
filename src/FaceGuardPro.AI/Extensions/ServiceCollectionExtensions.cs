using FaceGuardPro.AI.Configuration;
using FaceGuardPro.AI.Interfaces;
using FaceGuardPro.AI.Services;
using FaceGuardPro.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FaceGuardPro.AI.Extensions
{
    /// <summary>
    /// AI services dependency injection configuration
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add Face AI services to DI container
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Configuration</param>
        /// <returns>Service collection</returns>
        public static IServiceCollection AddFaceAIServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Face Detection Configuration
            var faceConfig = configuration.GetSection("FaceDetection").Get<FaceDetectionConfig>()
                ?? FaceDetectionConfig.Default;

            // Environment-specific configuration
            var environment = configuration["Environment"] ?? "Development";
            faceConfig = environment switch
            {
                "Production" => FaceDetectionConfig.HighAccuracy,
                "Testing" => FaceDetectionConfig.FastProcessing,
                _ => FaceDetectionConfig.Default
            };

            // Override with appsettings values if provided
            var configSection = configuration.GetSection("FaceDetection");
            if (configSection.Exists())
            {
                configSection.Bind(faceConfig);
            }

            services.AddSingleton(faceConfig);

            // Register AI services
            services.AddSingleton<IFaceDetectionService, FaceDetectionService>();
            services.AddSingleton<IFaceRecognitionService, FaceRecognitionService>();

            // Replace stub with real implementation
            services.AddScoped<IFaceEngineService, FaceEngineService>();

            // Initialize models on startup
            services.AddHostedService<FaceAIInitializationService>();

            return services;
        }

        /// <summary>
        /// Add Face AI services with custom configuration
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configureOptions">Configuration action</param>
        /// <returns>Service collection</returns>
        public static IServiceCollection AddFaceAIServices(
            this IServiceCollection services,
            Action<FaceDetectionConfig> configureOptions)
        {
            var config = FaceDetectionConfig.Default;
            configureOptions(config);

            services.AddSingleton(config);
            services.AddSingleton<IFaceDetectionService, FaceDetectionService>();
            services.AddSingleton<IFaceRecognitionService, FaceRecognitionService>();
            services.AddScoped<IFaceEngineService, FaceEngineService>();
            services.AddHostedService<FaceAIInitializationService>();

            return services;
        }
    }

    /// <summary>
    /// Background service for initializing Face AI models
    /// </summary>
    public class FaceAIInitializationService : IHostedService
    {
        private readonly ILogger<FaceAIInitializationService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public FaceAIInitializationService(
            ILogger<FaceAIInitializationService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Initializing Face AI services...");

                // Initialize face detection models
                FaceGuardPro.AI.Models.FaceDetectionModels.Initialize();
                _logger.LogInformation("Face detection models initialized");

                // Warm up services by creating instances
                using var scope = _serviceProvider.CreateScope();

                var faceDetectionService = scope.ServiceProvider.GetRequiredService<IFaceDetectionService>();
                var faceRecognitionService = scope.ServiceProvider.GetRequiredService<IFaceRecognitionService>();
                var faceEngineService = scope.ServiceProvider.GetRequiredService<IFaceEngineService>();

                // Perform health checks
                var detectionHealth = await faceDetectionService.HealthCheckAsync();
                var recognitionHealth = await faceRecognitionService.HealthCheckAsync();
                var engineHealth = await faceEngineService.HealthCheckAsync();

                _logger.LogInformation("Face AI services health check: Detection={DetectionHealth}, Recognition={RecognitionHealth}, Engine={EngineHealth}",
                    detectionHealth, recognitionHealth, engineHealth);

                if (!detectionHealth || !recognitionHealth || !engineHealth)
                {
                    _logger.LogWarning("Some Face AI services failed health check");
                }
                else
                {
                    _logger.LogInformation("Face AI services initialized successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Face AI services");
                // Don't throw here as it would prevent the application from starting
                // Services will handle errors gracefully when called
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Face AI services shutting down");
            return Task.CompletedTask;
        }
    }
}