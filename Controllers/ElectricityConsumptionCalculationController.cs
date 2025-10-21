using Azure.Core;
using GasForecast.Data;
using GasForecast.Models;
using GasForecast.Models.DTO;
using GasForecast.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace GasForecast.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ElectricityConsumptionCalculationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IElectricityConsumptionCalculator _calculator;

        public ElectricityConsumptionCalculationController(
            ApplicationDbContext context, IElectricityConsumptionCalculator calculator)
        {
            _context = context;
            _calculator = calculator;
        }

        [HttpGet("calculate_electricity_gas_consumption")]
        [Authorize]

        public async Task<ActionResult<IEnumerable<ElectricityCalculationResponseDTO>>> CalculateElectricityGasConsumption(ElectricityCalculationRequestDTO request)
        {
            try
            {
                var existingStation = await _context.ElectricalPowerStations
                    .FirstOrDefaultAsync(s => s.Id == request.StationId);

                if (existingStation == null)
                {
                    return Conflict($"ЭСН с ID '{request.StationId}' не найдена");
                }


                var OutsideTemperature = request.OutsideTemperature;
                var OperatingHours = request.OperatingHours;
                var UnitPowerPercentage = request.UnitPowerPercentage;
                var LowerHeatingValue = request.LowerHeatingValue;

                var UnitType = existingStation.UnitType;
                var ActiveUnitsCount = existingStation.ActiveUnitsCount;
                var unitPassport = await _context.ElectricalUnitPassports
                    .FirstOrDefaultAsync(s => s.UnitType == existingStation.UnitType);

                if (unitPassport == null)
                {
                    throw new ArgumentException($"Не найден паспорт для типа установки '{existingStation.UnitType}'");
                }

                var StandartPower = unitPassport.StandartPower;
                var GasConsumptionNorm = unitPassport.ConsumptionNorm;
                var TotalOperatingHours = (DateTime.Now - existingStation.LaunchDate).TotalHours;

                var EngineType = unitPassport.EngineType;

                var result = _calculator.CalculateGasConsumption(StandartPower, GasConsumptionNorm, OutsideTemperature, 
                    TotalOperatingHours, OperatingHours, UnitPowerPercentage, 
                    UnitType, ActiveUnitsCount, LowerHeatingValue, EngineType);

                var response = new ElectricityCalculationResponseDTO
                {
                    GasConsumption = result,
                    CalculationTime = DateTime.Now
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка: {ex.Message}");
            }
        }

    }
}
