using GasForecast.Models;
using GasForecast.Models.DTO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GasForecast.Services
{
    public interface IElectricityConsumptionCalculator
    {
        double CalculateGasConsumption(
            double standartPower,
            double gasConsumptionNorm,
            double outsideTemperature,
            double totalOperatingHours,
            double operatingHours,
            double unitPowerPercentage,
            string unitType,
            int activeUnitsCount,
            double lowerHeatingValue,
            string EngineType);
    }

    public class ElectricityConsumptionCalculator : IElectricityConsumptionCalculator
    {
        private readonly ElectricityCoefficientsService _coefficientService;
        public ElectricityConsumptionCalculator(ElectricityCoefficientsService coefficientService)
        {
            _coefficientService = coefficientService;
        }

        public double CalculateGasConsumption(
            double StandartPower,
            double GasConsumptionNorm,
            double OutsideTemperature,
            double TotalOperatingHours,
            double OperatingHours,
            double CurrentPowerPercentage,
            string UnitType,
            int ActiveUnitsCount,
            double LowerHeatingValue,
            string EngineType)
        {

            // Получаем нормы из сервиса
            var AtmosphericCoefficient = _coefficientService.GetTemperatureCoefficient(OutsideTemperature);
            var OperatingHoursCoefficient = _coefficientService.GetOperatingTimeCoefficient(TotalOperatingHours);
            var PowerCoefficient = _coefficientService.GetPowerCoefficient(EngineType, CurrentPowerPercentage);


            // Проверяем, что все необходимые данные есть
            if (StandartPower == null || GasConsumptionNorm == null ||
                AtmosphericCoefficient == null || OperatingHoursCoefficient == null)
            {
                throw new ArgumentException("Не удалось получить нормативные данные для расчета");
            }

            // Основная формула расчета расхода газа
            double gasConsumption = CalculateGasConsumptionFormula(
                OperatingHours,
                StandartPower,
                GasConsumptionNorm,
                AtmosphericCoefficient,
                OperatingHoursCoefficient,
                PowerCoefficient,
                ActiveUnitsCount,
                LowerHeatingValue,
                EngineType
            );

            return gasConsumption;
        }

        private double CalculateGasConsumptionFormula(
            double OperatingHours,
            double standartPower,
            double GasConsumptionNorm,
            double AtmosphericCoefficient,
            double OperatingHoursCoefficient,
            double PowerCoefficient,
            int ActiveUnitsCount,
            double LowerHeatingValue,
            string EngineType)
        {

            double correctionK = 1.1 * AtmosphericCoefficient * OperatingHoursCoefficient * PowerCoefficient;

            double calorieK = LowerHeatingValue / 7000;

            double finalConsumption = standartPower * GasConsumptionNorm * correctionK * ActiveUnitsCount * OperatingHours * 0.001 / calorieK;

            return Math.Round(finalConsumption, 3);
        }
    }
}
