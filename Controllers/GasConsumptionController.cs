using GasForecast.Data;
using GasForecast.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace GasForecast.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class GasConsumptionController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public GasConsumptionController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Вспомогательный метод для получения ID текущего пользователя из JWT
        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("id")?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }
            return null;
        }

        // Проверка доступа к станции
        private async Task<bool> HasAccessToStation(int stationId, int userId)
        {
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
            bool isAdmin = roleClaim?.Equals("admin", StringComparison.OrdinalIgnoreCase) == true;

            if (isAdmin) return true;
            return await _context.UserElectricalStations
                .AnyAsync(ues => ues.UserId == userId && ues.ElectricalStationId == stationId);
        }

        // 1) Чтение всех данных для ЭСН
        [HttpGet("read/{stationId}")]
        public async Task<ActionResult<IEnumerable<DailyGasConsumption>>> GetByStation(int stationId)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            if (!await HasAccessToStation(stationId, userId.Value))
                return Forbid();

            var consumptions = await _context.DailyGasConsumptions
                .Where(c => c.ElectricalStationId == stationId)
                .OrderBy(c => c.Date)
                .ToListAsync();

            return Ok(consumptions);
        }

        // 2) Добавление с проверкой - если существует, вернуть ошибку
        [HttpPost("create")]
        public async Task<ActionResult<DailyGasConsumption>> Create([FromBody] GasConsumptionCreateDto createDto)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            if (!await HasAccessToStation(createDto.ElectricalStationId, userId.Value))
                return Forbid();

            // Проверяем существование записи
            var exists = await _context.DailyGasConsumptions
                .AnyAsync(c => c.ElectricalStationId == createDto.ElectricalStationId
                            && c.Date.Date == createDto.Date.Date);

            if (exists)
            {
                return Conflict(new
                {
                    message = "Запись за эту дату уже существует",
                    stationId = createDto.ElectricalStationId,
                    date = createDto.Date.Date
                });
            }

            var consumption = new DailyGasConsumption
            {
                ElectricalStationId = createDto.ElectricalStationId,
                Date = createDto.Date,
                Consumption = createDto.Consumption
            };

            _context.DailyGasConsumptions.Add(consumption);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = consumption.Id }, consumption);
        }

        // 3) Добавление с перезаписью - если существует, обновить существующую
        [HttpPost("create-or-update")]
        public async Task<ActionResult<DailyGasConsumption>> CreateOrUpdate([FromBody] GasConsumptionCreateDto createDto)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            if (!await HasAccessToStation(createDto.ElectricalStationId, userId.Value))
                return Forbid();

            var existing = await _context.DailyGasConsumptions
                .FirstOrDefaultAsync(c => c.ElectricalStationId == createDto.ElectricalStationId
                                       && c.Date.Date == createDto.Date.Date);

            if (existing != null)
            {
                // Обновляем существующую запись
                existing.Consumption = createDto.Consumption;
                await _context.SaveChangesAsync();
                return Ok(existing);
            }
            else
            {
                // Создаем новую
                var consumption = new DailyGasConsumption
                {
                    ElectricalStationId = createDto.ElectricalStationId,
                    Date = createDto.Date,
                    Consumption = createDto.Consumption
                };

                _context.DailyGasConsumptions.Add(consumption);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetById), new { id = consumption.Id }, consumption);
            }
        }

        // 4) Массовое добавление с проверкой - если есть хоть один существующий, отмена всего
        [HttpPost("bulk-create")]
        public async Task<ActionResult<List<DailyGasConsumption>>> CreateBulk(int stationId, [FromBody] List<GasConsumptionCreateDto> createDtos)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            if (!await HasAccessToStation(stationId, userId.Value))
                return Forbid();

            if (createDtos == null || createDtos.Count == 0)
                return BadRequest(new { message = "Добавьте хотя бы одну запись" });

            // Проверяем дубликаты дат внутри запроса
            var duplicateDatesInRequest = createDtos
                .GroupBy(c => c.Date.Date)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key.ToShortDateString())
                .ToList();

            if (duplicateDatesInRequest.Any())
            {
                return BadRequest(new
                {
                    message = $"Обнаружены дубликаты дат в запросе",
                    duplicateDates = duplicateDatesInRequest
                });
            }

            // Проверяем существующие записи в БД
            var existingDates = await _context.DailyGasConsumptions
                .Where(c => c.ElectricalStationId == stationId
                            && createDtos.Select(x => x.Date.Date).Contains(c.Date.Date))
                .Select(c => c.Date.Date)
                .ToListAsync();

            if (existingDates.Any())
            {
                return Conflict(new
                {
                    message = "Некоторые даты уже существуют. Операция отменена.",
                    existingDates = existingDates.Select(d => d.ToShortDateString()),
                    suggestion = "Используйте bulk-create-or-update для перезаписи существующих"
                });
            }

            // Создаем новые записи
            var consumptions = createDtos.Select(dto => new DailyGasConsumption
            {
                ElectricalStationId = stationId,
                Date = dto.Date,
                Consumption = dto.Consumption
            }).ToList();

            _context.DailyGasConsumptions.AddRange(consumptions);
            await _context.SaveChangesAsync();

            return Ok(consumptions);
        }

        // 5) Массовое добавление с перезаписью - существующие обновляются, новые добавляются
        [HttpPost("bulk-create-or-update")]
        public async Task<ActionResult<object>> CreateBulkOrUpdate(int stationId, [FromBody] List<GasConsumptionCreateDto> createDtos)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            if (!await HasAccessToStation(stationId, userId.Value))
                return Forbid();

            if (createDtos == null || createDtos.Count == 0)
                return BadRequest(new { message = "Добавьте хотя бы одну запись" });

            // Проверяем дубликаты дат внутри запроса
            var duplicateDatesInRequest = createDtos
                .GroupBy(c => c.Date.Date)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key.ToShortDateString())
                .ToList();

            if (duplicateDatesInRequest.Any())
            {
                return BadRequest(new
                {
                    message = $"Обнаружены дубликаты дат в запросе",
                    duplicateDates = duplicateDatesInRequest
                });
            }

            var created = new List<DailyGasConsumption>();
            var updated = new List<DailyGasConsumption>();

            foreach (var dto in createDtos)
            {
                var existing = await _context.DailyGasConsumptions
                    .FirstOrDefaultAsync(c => c.ElectricalStationId == stationId
                                           && c.Date.Date == dto.Date.Date);

                if (existing != null)
                {
                    // Обновляем существующую
                    existing.Consumption = dto.Consumption;
                    updated.Add(existing);
                }
                else
                {
                    // Добавляем новую
                    var consumption = new DailyGasConsumption
                    {
                        ElectricalStationId = stationId,
                        Date = dto.Date,
                        Consumption = dto.Consumption
                    };
                    _context.DailyGasConsumptions.Add(consumption);
                    created.Add(consumption);
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Создано: {created.Count}, Обновлено: {updated.Count}",
                created,
                updated
            });
        }

        // 6) Удаление значения
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var consumption = await _context.DailyGasConsumptions.FindAsync(id);
            if (consumption == null)
                return NotFound();

            if (!await HasAccessToStation(consumption.ElectricalStationId, userId.Value))
                return Forbid();

            _context.DailyGasConsumptions.Remove(consumption);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // 7) Массовое удаление по датам
        [HttpDelete("bulk-delete")]
        public async Task<IActionResult> DeleteBulk(int stationId, [FromBody] List<DateTime> dates)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            if (!await HasAccessToStation(stationId, userId.Value))
                return Forbid();

            var consumptions = await _context.DailyGasConsumptions
                .Where(c => c.ElectricalStationId == stationId
                            && dates.Select(d => d.Date).Contains(c.Date.Date))
                .ToListAsync();

            if (consumptions.Count == 0)
                return NotFound();

            _context.DailyGasConsumptions.RemoveRange(consumptions);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Удалено записей: {consumptions.Count}",
                deletedCount = consumptions.Count
            });
        }

        // 8) Редактирование конкретной записи по ID
        [HttpPut("edit/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] GasConsumptionUpdateDto updateDto)
        {
            if (id != updateDto.Id)
                return BadRequest(new { message = "ID в URL и теле запроса не совпадают" });

            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var existingConsumption = await _context.DailyGasConsumptions.FindAsync(id);
            if (existingConsumption == null)
                return NotFound();

            if (!await HasAccessToStation(existingConsumption.ElectricalStationId, userId.Value))
                return Forbid();

            // Проверяем, нет ли другой записи за эту дату (исключая текущую)
            var duplicateExists = await _context.DailyGasConsumptions
                .AnyAsync(c => c.ElectricalStationId == existingConsumption.ElectricalStationId
                            && c.Date.Date == updateDto.Date.Date
                            && c.Id != id);

            if (duplicateExists)
            {
                return Conflict(new
                {
                    message = "Запись за эту дату уже существует у другой записи",
                    date = updateDto.Date.Date
                });
            }

            existingConsumption.Date = updateDto.Date;
            existingConsumption.Consumption = updateDto.Consumption;

            await _context.SaveChangesAsync();

            return Ok(existingConsumption);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<DailyGasConsumption>> GetById(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var consumption = await _context.DailyGasConsumptions.FindAsync(id);
            if (consumption == null)
                return NotFound();

            if (!await HasAccessToStation(consumption.ElectricalStationId, userId.Value))
                return Forbid();

            return Ok(consumption);
        }
    }
}