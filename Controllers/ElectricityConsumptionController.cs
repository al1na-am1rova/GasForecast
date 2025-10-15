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

        [HttpGet("read_all_stations")]
        public async Task<ActionResult<IEnumerable<ElectricalPowerStation>>> GetAllStations()
        {
            try
            {
                var stations = await _context.ElectricalPowerStations
                    .OrderBy(s => s.Id)
                    .ToListAsync();

                return Ok(stations);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при получении списка ЭСН: {ex.Message}");
            }
        }

        [HttpPost("add_electrical_station")]
        [Authorize(Roles="admin")]
        public async Task<ActionResult<ElectricalPowerStation>> CreateStation([FromBody] CreateStationRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Проверяем, существует ли уже станция с таким названием
                var existingStation = await _context.ElectricalPowerStations
                    .FirstOrDefaultAsync(s => s.Name == request.Name);

                if (existingStation != null)
                {
                    return Conflict($"ЭСН с названием '{request.Name}' уже существует");
                }

                // Создаем новую станцию
                var station = new ElectricalPowerStation
                {
                    Name = request.Name,
                    UnitType = request.UnitType,
                    ActiveUnitsCount = request.ActiveUnitsCount,
                    LaunchDate = request.LaunchDate,
                };

                // Добавляем в базу данных
                _context.ElectricalPowerStations.Add(station);
                await _context.SaveChangesAsync();

                // Возвращаем созданную станцию с ID
                return CreatedAtAction(
                    nameof(GetStationById),
                    new { id = station.Id },
                    station);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при создании ЭСН: {ex.Message}");
            }
        }

        [HttpGet("get_station_by_id/{id}")]
        public async Task<ActionResult<ElectricalPowerStation>> GetStationById(int id)
        {
            try
            {
                var station = await _context.ElectricalPowerStations
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (station == null)
                {
                    return NotFound($"ЭСН с ID {id} не найдена");
                }

                return Ok(station);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при получении ЭСН: {ex.Message}");
            }
        }





    }
}