// Controllers/ElectricityConsumptionController.cs
using GasForecast.Data;
using GasForecast.Models;
using GasForecast.Models.DTO;
using GasForecast.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GasForecast.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ElectricityConsumptionController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IElectricityConsumptionCalculator _calculator;

        public ElectricityConsumptionController(
            ApplicationDbContext context,
            IElectricityConsumptionCalculator calculator)
        {
            _context = context;
            _calculator = calculator;
        }

        // GET: api/electricityconsumption - READ ALL
        [HttpGet ("read_all")]
        public async Task<ActionResult<IEnumerable<ElectricityConsumptionData>>> GetElectricityConsumptionData()
        {
            return await _context.ElectricityConsumptionData
                .OrderByDescending(e => e.Timestamp)
                .ToListAsync();
        }

        // GET: api/electricityconsumption/ READ BY ID
        [HttpGet("read_{id}")]
        public async Task<ActionResult<ElectricityConsumptionData>> GetElectricityConsumptionData(int id)
        {
            var data = await _context.ElectricityConsumptionData.FindAsync(id);
            if (data == null) return NotFound();
            return data;
        }

        //// POST: api/electricityconsumption - CREATE (без расчета)
        //[HttpPost("create")]
        //public async Task<ActionResult<ElectricityConsumptionData>> PostElectricityConsumptionData(
        //    ElectricityConsumptionData data)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    data.Timestamp = DateTime.UtcNow;
        //    _context.ElectricityConsumptionData.Add(data);
        //    await _context.SaveChangesAsync();

        //    return CreatedAtAction(nameof(GetElectricityConsumptionData),
        //        new { id = data.Id }, data);
        //}

        //// POST: api/electricityconsumption/calculate - Только расчет
        //[HttpPost("calculate")]
        //public ActionResult<ElectricityCalculationResponse> CalculateGasConsumption(
        //    [FromBody] ElectricityCalculationRequest request)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    try
        //    {
        //        // Создаем данные для расчета
        //        var calculationData = _calculator.CreateCalculationData(
        //            request.UnitType,
        //            request.ActiveUnitsCount,
        //            request.OutsideTemperature,
        //            request.OperatingHours,
        //            request.TotalOperatingHours,
        //            request.UnitPowerPercentage,
        //            request.LowerHeatingValue
        //        );

        //        // Рассчитываем расход газа
        //        var gasConsumption = _calculator.CalculateGasConsumption(calculationData);

        //        var response = new ElectricityCalculationResponse
        //        {
        //            GasConsumption = gasConsumption,
        //            Unit = "м³",
        //            CalculationTime = DateTime.UtcNow,
        //            CalculationId = Guid.NewGuid().ToString()
        //        };

        //        return Ok(response);
        //    }
        //    catch (ArgumentException ex)
        //    {
        //        return BadRequest(new { error = ex.Message });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { error = "Ошибка расчета" });
        //    }
        //}

        // POST: api/electricityconsumption/calculate-and-save - Расчет и сохранение

        [HttpPost("create_and_calculate")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<ElectricityConsumptionData>> CalculateAndSave(
            [FromBody] ElectricityCalculationRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Создаем данные для расчета
                var calculationData = _calculator.CreateCalculationData(
                    request.UnitType,
                    request.ActiveUnitsCount,
                    request.OutsideTemperature,
                    request.OperatingHours,
                    request.TotalOperatingHours,
                    request.UnitPowerPercentage,
                    request.LowerHeatingValue
                );

                // Рассчитываем расход газа
                var gasConsumption = _calculator.CalculateGasConsumption(calculationData);

                // Создаем полную запись для сохранения
                var consumptionData = new ElectricityConsumptionData
                {
                    Timestamp = DateTime.UtcNow,
                    UnitType = request.UnitType,
                    ActiveUnitsCount = request.ActiveUnitsCount,
                    OutsideTemperature = request.OutsideTemperature,
                    OperatingHours = request.OperatingHours,
                    TotalOperatingHours = request.TotalOperatingHours,
                    UnitPowerPercentage = request.UnitPowerPercentage,
                    LowerHeatingValue = request.LowerHeatingValue,
                    GasConsumption = gasConsumption
                };

                _context.ElectricityConsumptionData.Add(consumptionData);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetElectricityConsumptionData),
                    new { id = consumptionData.Id }, consumptionData);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Внутренняя ошибка сервера" });
            }
        }

        // PUT: api/electricityconsumption/ UPDATE с обязательным пересчетом
        [HttpPut("update_and_calculate_{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> PutElectricityConsumptionData(int id, ElectricityConsumptionData data)
        {
            if (id != data.Id) return BadRequest();

            try
            {
                // Всегда пересчитываем расход газа при обновлении
                var calculationData = _calculator.CreateCalculationData(
                    data.UnitType,
                    data.ActiveUnitsCount,
                    data.OutsideTemperature,
                    data.OperatingHours,
                    data.TotalOperatingHours,
                    data.UnitPowerPercentage,
                    data.LowerHeatingValue
                );

                // Рассчитываем новый расход газа
                var newGasConsumption = _calculator.CalculateGasConsumption(calculationData);
                data.GasConsumption = newGasConsumption;
                data.Timestamp = DateTime.UtcNow;

                _context.Entry(data).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Message = "Данные успешно обновлены с пересчетом расхода газа",
                    NewGasConsumption = data.GasConsumption
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ElectricityConsumptionDataExists(id)) return NotFound();
                else throw;
            }
        }

        // DELETE: api/electricityconsumption/DELETE
        [HttpDelete("delete_{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteElectricityConsumptionData(int id)
        {
            var data = await _context.ElectricityConsumptionData.FindAsync(id);
            if (data == null) return NotFound();
            _context.ElectricityConsumptionData.Remove(data);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // GET: api/electricityconsumption/unit-types - Доступные типы агрегатов
        [HttpGet("get_unit_types")]
        [Authorize]
        public ActionResult<IEnumerable<string>> GetUnitTypes()
        {
            var unitTypes = new List<string>
            {
                "ПГТЭС-1500-2Г", "ГТУ-2,5П", "ПАЭС-2500М", "ГТЭС-2,5", "ЭГ-2500",
                "ГТЭС-4", "ЭГ-6000", "БГТЭС-9,5", "ГТЭС-12", "ЭМ-16-25", "ГТЭ-25У",
                "АСГД-500", "ЭГ-500", "ДГ-98М"
            };
            return Ok(unitTypes);
        }

        //LINQ

        // Данные по конкретному типу агрегата
        [HttpGet("get_data_by_unit_type/{unitType}")]
        [Authorize]
        public IActionResult GetByUnitType(string unitType)
        {
            var result = _context.ElectricityConsumptionData
                .Where(data => data.UnitType == unitType)
                .Select(data => new
                {
                    data.Id,
                    data.Timestamp,
                    data.GasConsumption,
                    data.ActiveUnitsCount,
                    data.OutsideTemperature
                })
                .OrderByDescending(x => x.Timestamp)
                .ToList();

            return Ok(result);
        }

        //Анализ эффективности по типам агрегатов с временными интервалами
        [HttpGet("queries/unit_efficiency_analysis/{unitType}")]
        [Authorize]
        public IActionResult GetUnitEfficiencyAnalysis(string unitType, [FromQuery] int days = 30)
        {
            var startDate = DateTime.UtcNow.AddDays(-days);

            var result = _context.ElectricityConsumptionData
                .Where(data => data.UnitType == unitType && data.Timestamp >= startDate)
                .GroupBy(data => new {
                    Year = data.Timestamp.Year,
                    Month = data.Timestamp.Month,
                    Week = data.Timestamp.Day / 7 // Группировка по неделям
                })
                .Select(group => new
                {
                    Period = $"{group.Key.Year}-{group.Key.Month:00}-W{group.Key.Week + 1}",
                    StartDate = group.Min(g => g.Timestamp),
                    EndDate = group.Max(g => g.Timestamp),

                    // Основные метрики
                    TotalRecords = group.Count(),
                    TotalGasConsumption = group.Sum(g => g.GasConsumption),
                    TotalOperatingHours = group.Sum(g => g.OperatingHours),

                    // Эффективность
                    AvgEfficiency = group.Average(g => g.GasConsumption / g.OperatingHours),
                    MaxEfficiency = group.Max(g => g.GasConsumption / g.OperatingHours),
                    MinEfficiency = group.Min(g => g.GasConsumption / g.OperatingHours),

                    // Температурный анализ
                    AvgTemperature = group.Average(g => g.OutsideTemperature),
                    MinTemperature = group.Min(g => g.OutsideTemperature),
                    MaxTemperature = group.Max(g => g.OutsideTemperature),

                    // Анализ мощности
                    AvgPowerPercentage = group.Average(g => g.UnitPowerPercentage),

                    // Статистика по наработке
                    AvgOperatingHours = group.Average(g => g.OperatingHours),
                    TotalActiveUnits = group.Sum(g => g.ActiveUnitsCount),
                })
                .OrderBy(x => x.StartDate)
                .ToList();

            return Ok(new { UnitType = unitType, AnalysisPeriod = $"{days} дней", Data = result });
        }

        private bool ElectricityConsumptionDataExists(int id)
        {
            return _context.ElectricityConsumptionData.Any(e => e.Id == id);
        }
    }
}