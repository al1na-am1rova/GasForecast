// Services/ML/ModelTrainingBackgroundService.cs
using Microsoft.Extensions.Hosting;

namespace GasForecast.Services.ML
{
    public class ModelTrainingBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ModelTrainingBackgroundService> _logger;
        private readonly IConfiguration _configuration;
        private readonly TimeSpan _checkInterval;

        public ModelTrainingBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<ModelTrainingBackgroundService> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;

            _checkInterval = TimeSpan.FromHours(
                configuration.GetValue("MLService:RetrainingIntervalHours", 6)
            );
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Model training background service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_checkInterval, stoppingToken);
                    await CheckAndTrainModelsAsync(stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in model training background service");
                }
            }
        }

        private async Task CheckAndTrainModelsAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var modelManagement = scope.ServiceProvider.GetRequiredService<IModelManagementService>();
            var mlClient = scope.ServiceProvider.GetRequiredService<IMLServiceClient>();

            try
            {
                var health = await mlClient.GetHealthAsync();
                if (health.Status != "healthy")
                {
                    _logger.LogWarning("ML service is not healthy. Status: {Status}", health.Status);
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not check ML service health");
                return;
            }

            var stations = await modelManagement.GetStationsNeedingTrainingAsync();

            foreach (var stationId in stations)
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                try
                {
                    _logger.LogInformation("Starting automatic training for station {StationId}", stationId);
                    await modelManagement.TriggerTrainingAsync(stationId);
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to auto-train station {StationId}", stationId);
                }
            }
        }
    }
}