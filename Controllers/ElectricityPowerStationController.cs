// Controllers/ElectricityConsumptionController.cs
using GasForecast.Data;
using GasForecast.Models;
using GasForecast.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace GasForecast.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ElectricityPowerStationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ElectricityPowerStationController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("read_all_stations")]
        [Authorize]
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
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<ElectricalPowerStation>> CreateStation([FromBody] ElectricalPowerStationCreateDTO request)
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
                    LaunchDate = request.LaunchDate

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
        [Authorize]
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

        [HttpDelete("delete_station/{id}")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult> DeleteStation(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var station = await _context.ElectricalPowerStations
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (station == null)
                {
                    return NotFound($"ЭСН с ID {id} не найдена");
                }

                _context.ElectricalPowerStations.Remove(station);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new
                {
                    Message = $"ЭСН '{station.Name}' успешно удалена.",
                    StationId = id,
                    StationName = station.Name,
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Ошибка при удалении ЭСН: {ex.Message}");
            }
        }

        [HttpPut("update_electrical_station/{id}")]
        [Authorize(Roles = "admin")]

        public async Task<ActionResult<ElectricalPowerStation>> UpdateStation(int id, [FromBody] ElectricalPowerStationUpdateDTO request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Находим станцию по ID
                var station = await _context.ElectricalPowerStations
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (station == null)
                {
                    return NotFound($"ЭСН с ID {id} не найдена");
                }

                // Проверяем, существует ли другая станция с таким же названием (если имя изменилось)
                if (request.Name != null && station.Name != request.Name)
                {
                    var existingStationWithSameName = await _context.ElectricalPowerStations
                        .FirstOrDefaultAsync(s => s.Name == request.Name && s.Id != id);

                    if (existingStationWithSameName != null)
                    {
                        return Conflict($"ЭСН с названием '{request.Name}' уже существует");
                    }
                }

                if (request.Name != null) station.Name = request.Name;
                if (request.ActiveUnitsCount.HasValue) station.ActiveUnitsCount = request.ActiveUnitsCount.Value;
                if (request.UnitType != null) station.UnitType = request.UnitType;
                if (request.LaunchDate.HasValue) station.LaunchDate = request.LaunchDate.Value;

                // Сохраняем изменения
                await _context.SaveChangesAsync();

                return Ok(station);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при обновлении ЭСН: {ex.Message}");
            }
        }


        [HttpGet("get_passport_by_station_id/{id}")]
        [Authorize]
        public async Task<ActionResult> GetUnitPassportsByStationId(int id)
        {
            var result = await (from s in _context.ElectricalPowerStations
                                join u in _context.ElectricalUnitPassports
                                on s.UnitType equals u.UnitType
                                where s.Id == id
                                select new
                                {
                                    StationId = s.Id,
                                    StationName = s.Name,
                                    ActiveUnitsCount = s.ActiveUnitsCount,
                                    LaunchDate = s.LaunchDate,

                                    UnitId = u.Id,
                                    UnitType = u.UnitType,
                                    EngineType = u.EngineType,
                                    RatedPower = u.RatedPower,
                                    StandartPower = u.StandartPower,
                                    ConsumptionNorm = u.ConsumptionNorm
                                }).FirstOrDefaultAsync(); 

            if (result == null)
            {
                return NotFound($"ЭСН с ID {id} не найдена или для неё не найден паспорт агрегата");
            }

            return Ok(result);
        }

        [HttpGet("stations_grouped_by_unit_type")]
        public async Task<ActionResult> GetStationsGroupedByUnitType()
        {
            var result = await (from u in _context.ElectricalUnitPassports
                                join s in _context.ElectricalPowerStations
                                on u.UnitType equals s.UnitType into stationGroup
                                select new
                                {
                                    UnitType = u.UnitType,
                                    EngineType = u.EngineType,
                                    RatedPower = u.RatedPower,
                                    StationsCount = stationGroup.Count(),
                                    Stations = stationGroup.Select(s => new
                                    {
                                        s.Id,
                                        s.Name,
                                        s.ActiveUnitsCount,
                                        s.LaunchDate
                                    }).ToList()
                                }).ToListAsync();

            return Ok(result);
        }


    }
}