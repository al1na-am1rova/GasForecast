// Controllers/ElectricityConsumptionController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GasForecast.Data;
using GasForecast.Models;
using GasForecast.Models.DTO;
using GasForecast.Services;
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
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ElectricityConsumptionData>>> GetElectricityConsumptionData()
        {
            return await _context.ElectricityConsumptionData
                .OrderByDescending(e => e.Timestamp)
                .ToListAsync();
        }

        // GET: api/electricityconsumption/5 - READ BY ID
        [HttpGet("{id}")]
        public async Task<ActionResult<ElectricityConsumptionData>> GetElectricityConsumptionData(int id)
        {
            var data = await _context.ElectricityConsumptionData.FindAsync(id);
            if (data == null) return NotFound();
            return data;
        }

        // POST: api/electricityconsumption - CREATE (без расчета)
        [HttpPost]
        public async Task<ActionResult<ElectricityConsumptionData>> PostElectricityConsumptionData(
            ElectricityConsumptionData data)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            data.Timestamp = DateTime.UtcNow;
            _context.ElectricityConsumptionData.Add(data);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetElectricityConsumptionData),
                new { id = data.Id }, data);
        }

        // POST: api/electricityconsumption/calculate - Только расчет
        [HttpPost("calculate")]
        public ActionResult<ElectricityCalculationResponse> CalculateGasConsumption(
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

                var response = new ElectricityCalculationResponse
                {
                    GasConsumption = gasConsumption,
                    Unit = "м³",
                    CalculationTime = DateTime.UtcNow,
                    CalculationId = Guid.NewGuid().ToString()
                };

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Ошибка расчета" });
            }
        }

        // POST: api/electricityconsumption/calculate-and-save - Расчет и сохранение
        [HttpPost("calculate-and-save")]
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

        // PUT: api/electricityconsumption/5 - UPDATE (без пересчета)
        [HttpPut("{id}")]
        public async Task<IActionResult> PutElectricityConsumptionData(int id, ElectricityConsumptionData data)
        {
            if (id != data.Id) return BadRequest();

            data.Timestamp = DateTime.UtcNow;
            _context.Entry(data).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ElectricityConsumptionDataExists(id)) return NotFound();
                else throw;
            }
            return NoContent();
        }

        // DELETE: api/electricityconsumption/5 - DELETE
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteElectricityConsumptionData(int id)
        {
            var data = await _context.ElectricityConsumptionData.FindAsync(id);
            if (data == null) return NotFound();
            _context.ElectricityConsumptionData.Remove(data);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // GET: api/electricityconsumption/unit-types - Доступные типы агрегатов
        [HttpGet("unit-types")]
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

        private bool ElectricityConsumptionDataExists(int id)
        {
            return _context.ElectricityConsumptionData.Any(e => e.Id == id);
        }
    }
}