// Controllers/ElectricityConsumptionController.cs
using GasForecast.Data;
using GasForecast.Models;
using GasForecast.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;

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

        [HttpGet("read_all")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<ElectricalPowerStation>>> GetAllStations()
        {
            try
            {
                // ИСПРАВЛЕНО: ищем claim "id" (который мы добавили)
                var userIdClaim = User.FindFirst("id")?.Value;

                // Для отладки - посмотрим все claims
                Console.WriteLine("All claims:");
                foreach (var claim in User.Claims)
                {
                    Console.WriteLine($"- {claim.Type}: {claim.Value}");
                }

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int currentUserId))
                {
                    return Unauthorized("Не удалось определить пользователя");
                }

                // ПОЛУЧАЕМ РОЛЬ ПОЛЬЗОВАТЕЛЯ ИЗ JWT
                var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
                bool isAdmin = roleClaim?.Equals("admin", StringComparison.OrdinalIgnoreCase) == true;

                // ЕСЛИ АДМИН - ПОКАЗЫВАЕМ ВСЕ СТАНЦИИ
                if (isAdmin)
                {
                    var allStations = await _context.ElectricalPowerStations
                        .OrderBy(s => s.Id)
                        .ToListAsync();

                    return Ok(allStations); // Даже если пусто, возвращаем пустой массив
                }

                // ЕСЛИ ОБЫЧНЫЙ ПОЛЬЗОВАТЕЛЬ - ПОКАЗЫВАЕМ ТОЛЬКО ЕГО СТАНЦИИ
                var userStations = await _context.UserElectricalStations
                    .Where(ues => ues.UserId == currentUserId)
                    .Include(ues => ues.ElectricalPowerStation)
                    .Select(ues => ues.ElectricalPowerStation)
                    .Where(s => s != null)
                    .OrderBy(s => s.Id)
                    .ToListAsync();

                // ВСЕГДА ВОЗВРАЩАЕМ МАССИВ, ДАЖЕ ЕСЛИ ОН ПУСТОЙ
                return Ok(userStations);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при получении списка ЭСН: {ex.Message}");
            }
        }
        [HttpPost("add")]
        public async Task<ActionResult<ElectricalPowerStationCreateDTO>> CreateStation([FromBody] ElectricalPowerStationCreateDTO request)
        {
            try
            {
                // Проверка модели
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Получаем ID текущего пользователя
                var userIdClaim = User.FindFirst("id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int currentUserId))
                {
                    return Unauthorized("Не удалось определить пользователя");
                }

                // ПРОВЕРЯЕМ, ЕСТЬ ЛИ У ЭТОГО ПОЛЬЗОВАТЕЛЯ СТАНЦИЯ С ТАКИМ НАЗВАНИЕМ
                var existingStation = await _context.UserElectricalStations
                    .Where(ues => ues.UserId == currentUserId)
                    .Include(ues => ues.ElectricalPowerStation)
                    .Select(ues => ues.ElectricalPowerStation)
                    .AnyAsync(s => s.Name == request.Name);

                if (existingStation)
                {
                    return Conflict(new
                    {
                        message = $"У вас уже есть ЭСН с названием '{request.Name}'",
                        errorCode = "DUPLICATE_STATION_NAME"
                    });
                }

                // Создаем новую станцию
                var station = new ElectricalPowerStation
                {
                    Name = request.Name,
                    UnitType = request.UnitType,
                    ActiveUnitsCount = request.ActiveUnitsCount,
                    LaunchDate = request.LaunchDate
                };

                _context.ElectricalPowerStations.Add(station);
                await _context.SaveChangesAsync();

                // СОЗДАЕМ ЗАПИСЬ В ТАБЛИЦЕ СВЯЗИ
                var userStation = new UserElectricalStation
                {
                    UserId = currentUserId,
                    ElectricalStationId = station.Id
                };
                _context.UserElectricalStations.Add(userStation);
                await _context.SaveChangesAsync();

                // Возвращаем успех
                return StatusCode(201, new
                {
                    message = "ЭСН успешно создана",
                    station = new
                    {
                        request.Name,
                        request.UnitType,
                        request.ActiveUnitsCount,
                        request.LaunchDate
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при создании ЭСН: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
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

        [HttpDelete("delete/{id}")]
        [Authorize]
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

        [HttpPut("update/{id}")]
        [Authorize]
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