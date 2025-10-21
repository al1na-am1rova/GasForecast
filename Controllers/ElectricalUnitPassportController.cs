using GasForecast.Data;
using GasForecast.Models;
using GasForecast.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static System.Collections.Specialized.BitVector32;

namespace GasForecast.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ElectricalUnitPassportController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ElectricalUnitPassportController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("read_all_unit_passports")]
        [Authorize]

        public async Task<ActionResult<IEnumerable<ElectricalUnitPassport>>> GetElectricalUnitPassports()
        {
            try
            {
                var passports = await _context.ElectricalUnitPassports.ToListAsync();
                return Ok(passports);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Внутрення ошибка сервера: {ex.Message}");
            }
        }

        [HttpGet("get_passport_by_id/{id}")]
        [Authorize]
        public async Task<ActionResult<ElectricalUnitPassport>> GetElectricalUnitPassport(int id)
        {
            try
            {
                var passport = await _context.ElectricalUnitPassports.FindAsync(id);

                if (passport == null)
                {
                    return NotFound($"Паспорт электроагрегата с {id} не найден");
                }

                return Ok(passport);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Внутрення ошибка сервера: {ex.Message}");
            }
        }

        [HttpPost("create_unit_passport")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<ElectricalUnitPassport>> CreateElectricalUnitPassport(
    ElectricalUnitPassportCreateDTO createDto)
        {
            // Проверяем, существует ли уже станция с таким названием
            var existingStation = await _context.ElectricalUnitPassports
                .FirstOrDefaultAsync(s => s.UnitType == createDto.UnitType);

            if (existingStation != null)
            {
                return Conflict($"Паспорт агрегата типа '{createDto.UnitType}' уже существует");
            }

            var passport = new ElectricalUnitPassport
            {
                UnitType = createDto.UnitType,
                EngineType = createDto.EngineType,
                RatedPower = createDto.RatedPower,
                StandartPower = createDto.StandartPower,
                ConsumptionNorm = createDto.ConsumptionNorm
            };

            _context.ElectricalUnitPassports.Add(passport);
            await _context.SaveChangesAsync();

            //var responseDto = new ElectricalUnitPassportResponseDTO
            //{
            //    Id = passport.Id,
            //    UnitType = passport.UnitType,
            //    EngineType = passport.EngineType,
            //    RatedPower = passport.RatedPower,
            //    StandartPower = passport.StandartPower,
            //    ConsumptionNorm = passport.ConsumptionNorm
            //};

            return CreatedAtAction(nameof(GetElectricalUnitPassport), new { id = passport.Id }, passport);
        }

        [HttpPut("update_unit_passport/{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UpdateElectricalUnitPassport(int id, ElectricalUnitPassportUpdateDTO updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var existingPassport = await _context.ElectricalUnitPassports.FindAsync(id);
                if (existingPassport == null)
                {
                    return NotFound($"Паспорт электроагрегата с ID {id} не найден");
                }

                if (updateDto.UnitType != null)
                    existingPassport.UnitType = updateDto.UnitType;

                if (updateDto.EngineType != null)
                    existingPassport.EngineType = updateDto.EngineType;

                if (updateDto.RatedPower.HasValue)
                    existingPassport.RatedPower = updateDto.RatedPower.Value;

                if (updateDto.StandartPower.HasValue)
                    existingPassport.StandartPower = updateDto.StandartPower.Value;

                if (updateDto.ConsumptionNorm.HasValue)
                    existingPassport.ConsumptionNorm = updateDto.ConsumptionNorm.Value;

                await _context.SaveChangesAsync();

                return Ok(existingPassport);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Внутренняя ошибка сервера: {ex.Message}");
            }
        }


        [HttpDelete("delete_unit_passport_by_id/{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteElectricalUnitPassport(int id)
        {
            try
            {
                var passport = await _context.ElectricalUnitPassports.FindAsync(id);
                if (passport == null)
                {
                    return NotFound($"Паспорт электроагрегата с {id} не найден");
                }

                _context.ElectricalUnitPassports.Remove(passport);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Message = $"паспорт электроагрегата '{passport.UnitType}' успешно удален.",
                    PassportID = id
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Внутрення ошибка сервера: {ex.Message}");
            }
        }

    }
}