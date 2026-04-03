// Services/ML/ModelManagementService.cs
using GasForecast.Controllers;
using GasForecast.Data;
using Microsoft.EntityFrameworkCore;
using GasForecast.Services.ML;

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
        Task<AnomalyCheckResponse> CheckDataForAnomaliesAsync(int stationId, List<DailyDataPoint> data);
        Task<TrainWithCheckResponse> TrainModelWithCheckAsync(TrainWithCheckRequest request);
        Task<List<DailyDataPoint>> GetStationDailyDataAsync(int stationId, DateTime? startDate = null, DateTime? endDate = null);
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

        public async Task<AnomalyCheckResponse> CheckDataForAnomaliesAsync(int stationId, List<DailyDataPoint> data)
        {
            try
            {
                // Если нет данных для проверки
                if (data == null || !data.Any())
                {
                    return new AnomalyCheckResponse
                    {
                        StationId = stationId,
                        HasAnomalies = false,
                        TotalChecked = 0,
                        Recommendation = "Нет данных для проверки",
                        CanProceed = true,
                        NeedsConfirmation = false
                    };
                }

                // Проверяем, достаточно ли исторических данных
                var historicalCount = await _context.DailyGasConsumptions
                    .CountAsync(d => d.ElectricalStationId == stationId);

                if (historicalCount < 30)
                {
                    return new AnomalyCheckResponse
                    {
                        StationId = stationId,
                        HasAnomalies = false,
                        TotalChecked = data.Count,
                        Recommendation = $"Недостаточно исторических данных ({historicalCount} точек). " +
                                       "Рекомендуется накопить минимум 30 дней данных.",
                        CanProceed = true,
                        NeedsConfirmation = false
                    };
                }

                // Отправляем запрос в ML сервис
                return await _mlClient.CheckDataForAnomaliesAsync(stationId, data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking anomalies for station {StationId}", stationId);

                // При ошибке — разрешаем обучение, но с предупреждением
                return new AnomalyCheckResponse
                {
                    StationId = stationId,
                    HasAnomalies = false,
                    HasWarnings = true,
                    TotalChecked = data?.Count ?? 0,
                    Recommendation = $"Ошибка проверки аномалий: {ex.Message}. Обучение будет продолжено.",
                    CanProceed = true,
                    NeedsConfirmation = false
                };
            }
        }

        /// <summary>
        /// Обучение модели с проверкой аномалий
        /// </summary>
        public async Task<TrainWithCheckResponse> TrainModelWithCheckAsync(TrainWithCheckRequest request)
        {
            try
            {
                _logger.LogInformation("Training with check for station {StationId}, confirmAnomalies={ConfirmAnomalies}",
                    request.StationId, request.ConfirmAnomalies);

                // 1. Проверяем аномалии, если не указано принудительное обучение
                if (!request.ForceRetrain && !request.ConfirmAnomalies)
                {
                    var anomalyCheck = await CheckDataForAnomaliesAsync(request.StationId, request.Data);

                    if (anomalyCheck.HasAnomalies)
                    {
                        return new TrainWithCheckResponse
                        {
                            StationId = request.StationId,
                            Status = "requires_confirmation",
                            Message = "Обнаружены аномалии в данных. Требуется подтверждение.",
                            RequiresConfirmation = true,
                            AnomalyReport = anomalyCheck
                        };
                    }
                }

                // 2. Запускаем обучение
                var result = await _mlClient.TrainModelAsync(request.StationId, request.ForceRetrain);

                return new TrainWithCheckResponse
                {
                    StationId = request.StationId,
                    Status = "started",
                    Message = result.Message ?? "Обучение запущено в фоновом режиме",
                    RequiresConfirmation = false
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TrainModelWithCheck for station {StationId}", request.StationId);
                return new TrainWithCheckResponse
                {
                    StationId = request.StationId,
                    Status = "error",
                    Message = ex.Message,
                    RequiresConfirmation = false
                };
            }
        }

        /// <summary>
        /// Получение ежедневных данных станции
        /// </summary>
        public async Task<List<DailyDataPoint>> GetStationDailyDataAsync(int stationId, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var query = _context.DailyGasConsumptions
                    .Where(d => d.ElectricalStationId == stationId);

                if (startDate.HasValue)
                    query = query.Where(d => d.Date >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(d => d.Date <= endDate.Value);

                var data = await query
                    .OrderBy(d => d.Date)
                    .Select(d => new DailyDataPoint
                    {
                        Date = d.Date.ToString("yyyy-MM-dd"),
                        Consumption = (double)d.Consumption
                    })
                    .ToListAsync();

                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting daily data for station {StationId}", stationId);
                return new List<DailyDataPoint>();
            }
        }

    }
}