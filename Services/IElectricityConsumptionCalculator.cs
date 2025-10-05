using GasForecast.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GasForecast.Services
{
    public interface IElectricityConsumptionCalculator
    {
        double CalculateGasConsumption(ElectricityConsumptionData data);
        ElectricityConsumptionData CreateCalculationData(
            string unitType,
            int activeUnitsCount,
            double outsideTemperature,
            double operatingHours,
            double totalOperatingHours,
            double unitPowerPercentage,
            double lowerHeatingValue);
    }

    public class ElectricityConsumptionCalculator : IElectricityConsumptionCalculator
    {
        private readonly ElectricityConsumptionNorms _normsService;
        public ElectricityConsumptionCalculator(ElectricityConsumptionNorms normsService)
        {
            _normsService = normsService;
        }

        public double CalculateGasConsumption(ElectricityConsumptionData data)
        {
            // Получаем нормы из сервиса
            var availablePower = _normsService.GetAvailablePowerNorms(data.UnitType);
            var gasConsumptionNorm = _normsService.GetGasConsumptionNorm(data.UnitType);
            var atmosphericCoefficient = _normsService.GetAtmosphericCoefficients(data.OutsideTemperature);
            var operatingHoursCoefficient = _normsService.GetOperatingHoursRanges(data.TotalOperatingHours);
            var powerCoefficient = _normsService.GetPowerCoefficient(data.UnitType, data.UnitPowerPercentage);

            // Проверяем, что все необходимые данные есть
            if (availablePower == null || gasConsumptionNorm == null ||
                atmosphericCoefficient == null || operatingHoursCoefficient == null)
            {
                throw new ArgumentException("Не удалось получить нормативные данные для расчета");
            }

            // Основная формула расчета расхода газа
            double gasConsumption = CalculateGasConsumptionFormula(
                data.OperatingHours,
                availablePower.Value,
                gasConsumptionNorm.Value,
                atmosphericCoefficient.Value,
                operatingHoursCoefficient.Value,
                powerCoefficient,
                data.ActiveUnitsCount,
                data.LowerHeatingValue
            );

            return gasConsumption;
        }

        private double CalculateGasConsumptionFormula(
            double OperatingHours,
            double availablePower,
            double gasConsumptionNorm,
            double atmosphericCoefficient,
            double operatingHoursCoefficient,
            double powerCoefficient,
            int activeUnitsCount,
            double lowerHeatingValue)
        {
            double correctionK = 1.1 * atmosphericCoefficient * operatingHoursCoefficient * powerCoefficient;
            double calorieK = lowerHeatingValue / 7000;
            double finalConsumption = availablePower * gasConsumptionNorm * correctionK * activeUnitsCount * OperatingHours * 0.001 / calorieK;

            return Math.Round(finalConsumption, 3);
        }

        public ElectricityConsumptionData CreateCalculationData(
            string unitType,
            int activeUnitsCount,
            double outsideTemperature,
            double operatingHours,
            double totalOperatingHours,
            double unitPowerPercentage,
            double lowerHeatingValue)
        {
            return new ElectricityConsumptionData
            {
                UnitType = unitType,
                ActiveUnitsCount = activeUnitsCount,
                OutsideTemperature = outsideTemperature,
                OperatingHours = operatingHours,
                TotalOperatingHours = totalOperatingHours,
                UnitPowerPercentage = Math.Clamp(unitPowerPercentage, 0, 100),
                LowerHeatingValue = lowerHeatingValue,
                Timestamp = DateTime.Now
            };
        }
    }
}
