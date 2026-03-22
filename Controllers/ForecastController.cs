// Controllers/ForecastController.cs
using GasForecast.Data;
using GasForecast.Services.ML;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace GasForecast.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ForecastController : ControllerBase
    {
        private readonly IModelManagementService _modelManagement;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ForecastController> _logger;

        public ForecastController(
            IModelManagementService modelManagement,
            ApplicationDbContext context,
            ILogger<ForecastController> logger)
        {
            _modelManagement = modelManagement;
            _context = context;
            _logger = logger;
        }


        private async Task<bool> HasAccessToStation(int stationId, int userId)
        {
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
            bool isAdmin = roleClaim?.Equals("admin", StringComparison.OrdinalIgnoreCase) == true;

            if (isAdmin) return true;
            return await _context.UserElectricalStations
                .AnyAsync(ues => ues.UserId == userId && ues.ElectricalStationId == stationId);
        }

        // ==================== СУЩЕСТВУЮЩИЕ МЕТОДЫ ====================

        [HttpPost("predict/{stationId}")]
        public async Task<IActionResult> PredictGasConsumption(
            int stationId,
            [FromBody] ForecastRequest request)
        {
            // Проверяем доступ к станции
            var userId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            if (!await HasAccessToStation(stationId, userId))
                return Forbid();

            var predictionRequest = new PredictionRequest
            {
                StationId = stationId,
                Date = request.Date,
                Temperature = request.Temperature,
                ElectricityLoad = request.ElectricityLoad,
                IsWeekend = request.Date.DayOfWeek == DayOfWeek.Saturday ||
                            request.Date.DayOfWeek == DayOfWeek.Sunday
            };

            var result = await _modelManagement.PredictGasConsumptionAsync(predictionRequest);

            if (result.Status == "model_not_found")
                return Accepted(result);

            return Ok(result);
        }

        [HttpPost("train/{stationId}")]
        public async Task<IActionResult> TrainModel(int stationId, [FromQuery] bool force = false)
        {
            // Проверяем права админа
            var userId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            if (!await HasAccessToStation(stationId, userId))
                return Forbid();

            await _modelManagement.TriggerTrainingAsync(stationId, force);
            return Accepted(new { message = $"Training started for station {stationId}" });
        }

        [HttpGet("models/{stationId}/info")]
        public async Task<IActionResult> GetModelInfo(int stationId)
        {
            var info = await _modelManagement.GetModelInfoAsync(stationId);
            return Ok(info);
        }

        [HttpGet("health")]
        public async Task<IActionResult> GetMLServiceHealth()
        {
            return Ok(new { status = "ok" });
        }

        // ==================== НОВЫЕ МЕТОДЫ ====================

        /// <summary>
        /// Получить комбинированные данные (история + прогноз)
        /// </summary>
        [HttpGet("{stationId}/combined")]
        public async Task<IActionResult> GetCombinedData(int stationId, [FromQuery] int forecastMonths = 3)
        {
            try
            {
                _logger.LogInformation("GetCombinedData called for station {StationId}", stationId);

                // Проверяем доступ к станции
                var userId = int.Parse(User.FindFirst("id")?.Value ?? "0");
                if (!await HasAccessToStation(stationId, userId))
                    return Forbid();

                // Получаем исторические данные (агрегированные по месяцам)
                var historicalData = new List<MonthlyForecastDto>();

                try
                {
                    // Сначала получаем данные из БД без форматирования
                    var rawData = await _context.DailyGasConsumptions
                        .Where(d => d.ElectricalStationId == stationId)
                        .GroupBy(d => new { d.Date.Year, d.Date.Month })
                        .Select(g => new
                        {
                            Year = g.Key.Year,
                            Month = g.Key.Month,
                            Sum = g.Sum(d => d.Consumption)
                        })
                        .OrderBy(x => x.Year)
                        .ThenBy(x => x.Month)
                        .ToListAsync();

                    // Форматируем в памяти
                    historicalData = rawData.Select(x => new MonthlyForecastDto
                    {
                        Month = $"{x.Year}-{x.Month:D2}",
                        Predicted = (double)x.Sum
                    }).ToList();

                    _logger.LogInformation("Loaded {Count} historical months for station {StationId}", historicalData.Count, stationId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting historical data for station {StationId}", stationId);
                    historicalData = new List<MonthlyForecastDto>();
                }

                // Получаем прогноз от ML сервиса
                var forecast = new List<MonthlyForecastDto>();
                try
                {
                    var mlForecast = await _modelManagement.GetMonthlyForecastAsync(stationId, forecastMonths);
                    forecast = mlForecast?.Select(f => new MonthlyForecastDto
                    {
                        Month = f.Month,
                        Predicted = f.Predicted
                    }).ToList() ?? new List<MonthlyForecastDto>();
                    _logger.LogInformation("Loaded {Count} forecast months for station {StationId}", forecast.Count, stationId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not get forecast from ML service");
                    forecast = new List<MonthlyForecastDto>();
                }

                // Получаем количество записей
                int dataPointsCount = 0;
                try
                {
                    dataPointsCount = await _context.DailyGasConsumptions
                        .CountAsync(d => d.ElectricalStationId == stationId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error counting data points");
                }

                // Получаем информацию о модели
                ModelInfo modelInfo = null;
                try
                {
                    modelInfo = await _modelManagement.GetModelInfoAsync(stationId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not get model info");
                    modelInfo = new ModelInfo { StationId = stationId, Exists = false, DataPoints = dataPointsCount };
                }

                var result = new CombinedDataDto
                {
                    Historical = historicalData,
                    Forecast = forecast,
                    ModelInfo = new ModelInfoDto
                    {
                        StationId = stationId,
                        Exists = modelInfo?.Exists ?? false,
                        Version = modelInfo?.Version,
                        LastTrained = modelInfo?.LastTrained,
                        DataPoints = dataPointsCount,
                        AccuracyMape = modelInfo?.AccuracyMape
                    }
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting combined data for station {StationId}", stationId);
                return StatusCode(500, new { message = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        /// <summary>
        /// Сделать прогноз на месяц
        /// </summary>
        [HttpPost("predict-month")]
        public async Task<IActionResult> PredictMonth([FromBody] MonthlyPredictionRequestDto request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("id")?.Value ?? "0");
                if (!await HasAccessToStation(request.StationId, userId))
                    return Forbid();

                var result = await _modelManagement.PredictMonthlyAsync(request.StationId, request.Month);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error predicting month");
                return StatusCode(500, new { message = "Prediction failed" });
            }
        }

        /// <summary>
        /// Получить статус обучения модели
        /// </summary>
        [HttpGet("training/{stationId}/status")]
        public async Task<IActionResult> GetTrainingStatus(int stationId)
        {
            var status = await _modelManagement.GetTrainingStatusAsync(stationId);
            return Ok(status);
        }
    }

    // ==================== DTO КЛАССЫ ====================

    public class ForecastRequest
    {
        public DateTime Date { get; set; }
        public float? Temperature { get; set; }
        public float? ElectricityLoad { get; set; }
    }

    public class MonthlyPredictionRequestDto
    {
        public int StationId { get; set; }
        public string Month { get; set; } // YYYY-MM
    }

    public class MonthlyForecastDto
    {
        public string Month { get; set; }
        public double Predicted { get; set; }
    }

    public class CombinedDataDto
    {
        public List<MonthlyForecastDto> Historical { get; set; }
        public List<MonthlyForecastDto> Forecast { get; set; }
        public ModelInfoDto ModelInfo { get; set; }
    }

    public class ModelInfoDto
    {
        public int StationId { get; set; }
        public bool Exists { get; set; }
        public string Version { get; set; }
        public DateTime? LastTrained { get; set; }
        public int DataPoints { get; set; }
        public double? AccuracyMape { get; set; }
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

    public class TrainingStatusDto
    {
        public int StationId { get; set; }
        public string Status { get; set; } // pending, training, completed, failed
        public string Message { get; set; }
        public int? Progress { get; set; }
        public string ModelVersion { get; set; }
    }
}