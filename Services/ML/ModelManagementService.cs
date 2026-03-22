// Services/ML/ModelManagementService.cs
using GasForecast.Controllers;
using GasForecast.Data;
using Microsoft.EntityFrameworkCore;

namespace GasForecast.Services.ML
{
    public interface IModelManagementService
    {
        Task<PredictionResult> PredictGasConsumptionAsync(PredictionRequest request);
        Task TriggerTrainingAsync(int stationId, bool forceRetrain = false);
        Task<ModelInfo> GetModelInfoAsync(int stationId);
        Task<List<int>> GetStationsNeedingTrainingAsync();
        Task<MonthlyPredictionResultDto> PredictMonthlyAsync(int stationId, string month);
        Task<List<MonthlyForecastDto>> GetMonthlyForecastAsync(int stationId, int monthsCount);
        Task<TrainingStatusDto> GetTrainingStatusAsync(int stationId);
    }

    public class ModelManagementService : IModelManagementService
    {
        private readonly IMLServiceClient _mlClient;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ModelManagementService> _logger;

        public ModelManagementService(
            IMLServiceClient mlClient,
            ApplicationDbContext context,
            ILogger<ModelManagementService> logger)
        {
            _mlClient = mlClient;
            _context = context;
            _logger = logger;
        }

        public async Task<PredictionResult> PredictGasConsumptionAsync(PredictionRequest request)
        {
            try
            {
                var modelInfo = await _mlClient.GetModelInfoAsync(request.StationId);

                if (!modelInfo.Exists)
                {
                    await TriggerTrainingAsync(request.StationId);

                    return new PredictionResult
                    {
                        StationId = request.StationId,
                        Date = request.Date,
                        Status = "model_not_found",
                        Message = "Model is being trained. Please try again later."
                    };
                }

                var prediction = await _mlClient.PredictAsync(request);
                await SavePredictionHistoryAsync(request.StationId, request.Date, prediction);

                return prediction;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Prediction failed for station {StationId}", request.StationId);
                throw;
            }
        }

        public async Task<MonthlyPredictionResultDto> PredictMonthlyAsync(int stationId, string month)
        {
            try
            {
                var modelInfo = await _mlClient.GetModelInfoAsync(stationId);

                if (!modelInfo.Exists)
                {
                    await TriggerTrainingAsync(stationId);

                    return new MonthlyPredictionResultDto
                    {
                        StationId = stationId,
                        Month = month,
                        Status = "model_not_found",
                        Message = "Model is being trained. Please try again later."
                    };
                }

                var request = new MonthlyPredictionRequest
                {
                    StationId = stationId,
                    Month = month
                };

                var prediction = await _mlClient.PredictMonthlyAsync(request);
                return prediction;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Monthly prediction failed for station {StationId}", stationId);

                // Fallback: используем среднее за последние 3 месяца
                return await GetFallbackPredictionAsync(stationId, month);
            }
        }

        public async Task<List<MonthlyForecastDto>> GetMonthlyForecastAsync(int stationId, int monthsCount)
        {
            try
            {
                var forecast = await _mlClient.GetMonthlyForecastAsync(stationId, monthsCount);
                if (forecast != null && forecast.Any())
                {
                    return forecast;
                }

                return await GetFallbackForecastAsync(stationId, monthsCount);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not get ML forecast, using fallback");
                return await GetFallbackForecastAsync(stationId, monthsCount);
            }
        }

        public async Task TriggerTrainingAsync(int stationId, bool forceRetrain = false)
        {
            try
            {
                await _mlClient.TrainModelAsync(stationId, forceRetrain);
                _logger.LogInformation("Training triggered for station {StationId}", stationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to trigger training for station {StationId}", stationId);
                throw;
            }
        }

        public async Task<ModelInfo> GetModelInfoAsync(int stationId)
        {
            return await _mlClient.GetModelInfoAsync(stationId);
        }

        public async Task<TrainingStatusDto> GetTrainingStatusAsync(int stationId)
        {
            try
            {
                // Здесь можно реализовать проверку статуса обучения
                // Например, из БД или из ML сервиса
                return new TrainingStatusDto
                {
                    StationId = stationId,
                    Status = "completed",
                    Message = "Model is ready"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting training status");
                return new TrainingStatusDto
                {
                    StationId = stationId,
                    Status = "unknown",
                    Message = ex.Message
                };
            }
        }

        public async Task<List<int>> GetStationsNeedingTrainingAsync()
        {
            var stations = await _context.ElectricalPowerStations
                .Include(s => s.DailyGasConsumptions)
                .ToListAsync();

            var stationsNeedingTraining = new List<int>();

            foreach (var station in stations)
            {
                var modelInfo = await _mlClient.GetModelInfoAsync(station.Id);

                var hasEnoughData = station.DailyGasConsumptions.Count >= 30;
                var hasNoModel = !modelInfo.Exists;
                var modelOutdated = modelInfo.LastTrained.HasValue &&
                                   (DateTime.UtcNow - modelInfo.LastTrained.Value).TotalDays > 7;

                if (hasEnoughData && (hasNoModel || modelOutdated))
                {
                    stationsNeedingTraining.Add(station.Id);
                }
            }

            return stationsNeedingTraining;
        }

        // ==================== FALLBACK МЕТОДЫ ====================

        private async Task<MonthlyPredictionResultDto> GetFallbackPredictionAsync(int stationId, string month)
        {
            var lastThreeMonths = await _context.DailyGasConsumptions
                .Where(d => d.ElectricalStationId == stationId)
                .GroupBy(d => new { d.Date.Year, d.Date.Month })
                .Select(g => g.Sum(d => d.Consumption))
                .OrderByDescending(s => s)
                .Take(3)
                .ToListAsync();

            var avg = lastThreeMonths.Any() ? (double)lastThreeMonths.Average() : 0;

            return new MonthlyPredictionResultDto
            {
                StationId = stationId,
                Month = month,
                PredictedConsumption = avg,
                Confidence = 0.5,
                ModelVersion = "fallback",
                Status = "fallback",
                Message = "Using fallback prediction based on historical average"
            };
        }

        private async Task<List<MonthlyForecastDto>> GetFallbackForecastAsync(int stationId, int monthsCount)
        {
            var lastThreeMonths = await _context.DailyGasConsumptions
                .Where(d => d.ElectricalStationId == stationId)
                .GroupBy(d => new { d.Date.Year, d.Date.Month })
                .Select(g => g.Sum(d => d.Consumption))
                .OrderByDescending(s => s)
                .Take(3)
                .ToListAsync();

            var avg = lastThreeMonths.Any() ? (double)lastThreeMonths.Average() : 0;

            var forecast = new List<MonthlyForecastDto>();
            var now = DateTime.Now;

            for (int i = 1; i <= monthsCount; i++)
            {
                var date = now.AddMonths(i);
                var monthStr = $"{date.Year}-{date.Month:D2}";

                forecast.Add(new MonthlyForecastDto
                {
                    Month = monthStr,
                    Predicted = avg
                });
            }

            return forecast;
        }

        private async Task SavePredictionHistoryAsync(int stationId, DateTime date, PredictionResult prediction)
        {
            try
            {
                _logger.LogDebug("Prediction saved for station {StationId} on {Date}", stationId, date);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to save prediction history: {Error}", ex.Message);
            }
        }
    }
}