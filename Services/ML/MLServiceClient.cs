// Services/ML/MLServiceClient.cs
using System.Text;
using System.Text.Json;

namespace GasForecast.Services.ML
{
    public interface IMLServiceClient
    {
        Task<PredictionResult> PredictAsync(PredictionRequest request);
        Task<TrainingResult> TrainModelAsync(int stationId, bool forceRetrain = false);
        Task<ModelInfo> GetModelInfoAsync(int stationId);
        Task<bool> IsModelReadyAsync(int stationId);
        Task<HealthStatus> GetHealthAsync();
        Task<MonthlyPredictionResultDto> PredictMonthlyAsync(MonthlyPredictionRequest request);
        Task<List<MonthlyForecastDto>> GetMonthlyForecastAsync(int stationId, int monthsCount);
        Task<AnomalyCheckResponse> CheckDataForAnomaliesAsync(int stationId, List<DailyDataPoint> data);
        Task<TrainWithCheckResponse> TrainModelWithCheckAsync(TrainWithCheckRequest request);
        Task<List<DailyDataPoint>> GetStationDailyDataAsync(int stationId, DateTime? startDate = null, DateTime? endDate = null);
    }

    public class MLServiceClient : IMLServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<MLServiceClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public MLServiceClient(HttpClient httpClient, ILogger<MLServiceClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        // Services/ML/MLServiceClient.cs

        public async Task<PredictionResult> PredictAsync(PredictionRequest request)
        {
            try
            {
                // Исправление: отправляем дату в формате YYYY-MM-DD без времени!
                var pythonRequest = new
                {
                    stationId = request.StationId,
                    date = request.Date.ToString("yyyy-MM-dd"),  // <-- ГЛАВНОЕ ИСПРАВЛЕНИЕ
                    temperature = request.Temperature,
                    electricity_load = request.ElectricityLoad,
                    is_weekend = request.IsWeekend
                };

                var json = JsonSerializer.Serialize(pythonRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/ml/predict", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<PredictionResult>(responseJson, _jsonOptions);
                }

                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("ML service error: {Error}", error);
                throw new HttpRequestException($"ML service returned {response.StatusCode}: {error}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling ML service for prediction");
                throw;
            }
        }
        public async Task<MonthlyPredictionResultDto> PredictMonthlyAsync(MonthlyPredictionRequest request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/ml/predict-month", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<MonthlyPredictionResultDto>(responseJson, _jsonOptions);

                    if (result != null)
                    {
                        result.Status = "success";
                    }

                    return result;
                }

                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("ML service error: {Error}", error);

                return new MonthlyPredictionResultDto
                {
                    StationId = request.StationId,
                    Month = request.Month,
                    PredictedConsumption = 0,
                    Confidence = 0,
                    ModelVersion = "error",
                    Status = "error",
                    Message = $"ML service returned {response.StatusCode}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling ML service for monthly prediction");

                return new MonthlyPredictionResultDto
                {
                    StationId = request.StationId,
                    Month = request.Month,
                    PredictedConsumption = 0,
                    Confidence = 0,
                    ModelVersion = "error",
                    Status = "error",
                    Message = ex.Message
                };
            }
        }

        public async Task<List<MonthlyForecastDto>> GetMonthlyForecastAsync(int stationId, int monthsCount)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/ml/forecast/{stationId}?months={monthsCount}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<MonthlyForecastDto>>(json, _jsonOptions)
                           ?? new List<MonthlyForecastDto>();
                }

                _logger.LogWarning("ML service returned {StatusCode} for monthly forecast", response.StatusCode);
                return new List<MonthlyForecastDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting monthly forecast from ML service");
                return new List<MonthlyForecastDto>();
            }
        }

        public async Task<TrainingResult> TrainModelAsync(int stationId, bool forceRetrain = false)
        {
            try
            {
                var request = new { station_id = stationId, force_retrain = forceRetrain };
                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

                var response = await _httpClient.PostAsync("/api/ml/train", content, cts.Token);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<TrainingResult>(responseJson, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting training for station {StationId}", stationId);
                throw;
            }
        }

        public async Task<ModelInfo> GetModelInfoAsync(int stationId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/ml/models/{stationId}");

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return new ModelInfo { StationId = stationId, Exists = false, DataPoints = 0 };
                }

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ModelInfo>(json, _jsonOptions);

                if (result != null)
                {
                    result.StationId = stationId;
                }

                return result ?? new ModelInfo { StationId = stationId, Exists = false, DataPoints = 0 };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting model info for station {StationId}", stationId);
                return new ModelInfo { StationId = stationId, Exists = false, DataPoints = 0 };
            }
        }

        public async Task<bool> IsModelReadyAsync(int stationId)
        {
            try
            {
                var info = await GetModelInfoAsync(stationId);
                return info.Exists && info.DataPoints >= 30;
            }
            catch
            {
                return false;
            }
        }

        public async Task<HealthStatus> GetHealthAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/health");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<HealthStatus>(json, _jsonOptions)
                       ?? new HealthStatus { Status = "unhealthy" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ML service health check failed");
                return new HealthStatus { Status = "unhealthy", ModelsLoaded = 0, DatabaseConnected = false };
            }
        }
        public async Task<AnomalyCheckResponse> CheckDataForAnomaliesAsync(int stationId, List<DailyDataPoint> data)
        {
            try
            {
                var request = new
                {
                    stationId = stationId,
                    data = data.Select(d => new { date = d.Date, consumption = d.Consumption })
                };

                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/ml/check-data", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<AnomalyCheckResponse>(responseJson, _jsonOptions)
                           ?? new AnomalyCheckResponse { StationId = stationId, HasAnomalies = false };
                }

                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Check data error: {Error}", error);

                return new AnomalyCheckResponse
                {
                    StationId = stationId,
                    HasAnomalies = false,
                    HasWarnings = false,
                    Recommendation = "Не удалось проверить данные на аномалии",
                    CanProceed = true,
                    NeedsConfirmation = false
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking data for anomalies for station {StationId}", stationId);
                return new AnomalyCheckResponse
                {
                    StationId = stationId,
                    HasAnomalies = false,
                    Recommendation = $"Ошибка проверки: {ex.Message}",
                    CanProceed = true
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
                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/ml/train-with-check", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<TrainWithCheckResponse>(responseJson, _jsonOptions)
                           ?? new TrainWithCheckResponse
                           {
                               StationId = request.StationId,
                               Status = "error",
                               Message = "Empty response from ML service"
                           };
                }

                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Train with check error: {Error}", error);

                return new TrainWithCheckResponse
                {
                    StationId = request.StationId,
                    Status = "error",
                    Message = $"ML service error: {response.StatusCode}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in train with check for station {StationId}", request.StationId);
                throw;
            }
        }

        /// <summary>
        /// Получение ежедневных данных станции
        /// </summary>
        public async Task<List<DailyDataPoint>> GetStationDailyDataAsync(int stationId, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var url = $"/api/data/{stationId}/daily";
                var queryParams = new List<string>();

                if (startDate.HasValue)
                    queryParams.Add($"startDate={startDate.Value:yyyy-MM-dd}");
                if (endDate.HasValue)
                    queryParams.Add($"endDate={endDate.Value:yyyy-MM-dd}");

                if (queryParams.Any())
                    url += "?" + string.Join("&", queryParams);

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<DailyDataPoint>>(json, _jsonOptions)
                           ?? new List<DailyDataPoint>();
                }

                _logger.LogWarning("Failed to get daily data for station {StationId}: {StatusCode}", stationId, response.StatusCode);
                return new List<DailyDataPoint>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting daily data for station {StationId}", stationId);
                return new List<DailyDataPoint>();
            }
        }
    }

    // ==================== МОДЕЛИ ДАННЫХ ====================

    public class PredictionRequest
    {
        public int StationId { get; set; }
        public DateTime Date { get; set; }
        public float? Temperature { get; set; }
        public float? ElectricityLoad { get; set; }
        public bool? IsWeekend { get; set; }
    }

    public class MonthlyPredictionRequest
    {
        public int StationId { get; set; }
        public string Month { get; set; }
    }

    public class PredictionResult
    {
        public int StationId { get; set; }
        public DateTime Date { get; set; }
        public float PredictedConsumption { get; set; }
        public float Confidence { get; set; }
        public string ModelVersion { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
    }

    public class MonthlyPredictionResultDto
    {
        public int StationId { get; set; }
        public string Month { get; set; }
        public double PredictedConsumption { get; set; }
        public double Confidence { get; set; }
        public string ModelVersion { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
    }

    public class MonthlyForecastDto
    {
        public string Month { get; set; }
        public double Predicted { get; set; }
    }

    public class TrainingResult
    {
        public int StationId { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public string ModelVersion { get; set; }
        public Dictionary<string, float> Metrics { get; set; }
    }

    public class ModelInfo
    {
        public int StationId { get; set; }
        public bool Exists { get; set; }
        public string Version { get; set; }
        public DateTime? LastTrained { get; set; }
        public int DataPoints { get; set; }
        public double? AccuracyMape { get; set; }
        public Dictionary<string, float> Metrics { get; set; }
        public Dictionary<string, float> FeatureImportance { get; set; }
    }

    public class HealthStatus
    {
        public string Status { get; set; }
        public int ModelsLoaded { get; set; }
        public string ModelsPath { get; set; }
        public bool DatabaseConnected { get; set; }
    }
    public class DailyDataPoint
    {
        public string Date { get; set; }
        public double Consumption { get; set; }
    }

    public class AnomalyPoint
    {
        public string Date { get; set; }
        public double Value { get; set; }
        public bool IsAnomaly { get; set; }
        public bool IsWarning { get; set; }
        public double? ZScore { get; set; }
        public double? HistoricalMean { get; set; }
        public double? HistoricalStd { get; set; }
        public int HistoryCount { get; set; }
        public string Message { get; set; }
    }

    public class AnomalyCheckResponse
    {
        public int StationId { get; set; }
        public bool HasAnomalies { get; set; }
        public bool HasWarnings { get; set; }
        public int TotalChecked { get; set; }
        public int AnomalyCount { get; set; }
        public int WarningCount { get; set; }
        public List<AnomalyPoint> Anomalies { get; set; }
        public string Recommendation { get; set; }
        public bool CanProceed { get; set; }
        public bool NeedsConfirmation { get; set; }
    }

    public class TrainWithCheckRequest
    {
        public int StationId { get; set; }
        public List<DailyDataPoint> Data { get; set; }
        public bool ForceRetrain { get; set; }
        public bool ConfirmAnomalies { get; set; }
    }

    public class TrainWithCheckResponse
    {
        public int StationId { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public bool? RequiresConfirmation { get; set; }
        public AnomalyCheckResponse AnomalyReport { get; set; }
    }
}