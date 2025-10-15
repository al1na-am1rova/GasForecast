using GasForecast.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GasForecast.Services
{
    public interface IElectricityConsumptionCalculator
    {

    }

    public class ElectricityConsumptionCalculator : IElectricityConsumptionCalculator
    {
        private readonly ElectricityCoefficientsService _coefficientService;
        public ElectricityConsumptionCalculator(ElectricityCoefficientsService coefficientService)
        {
            _coefficientService = coefficientService;
        }

        public double CalculateGasConsumption(int StandartPower,double GasConsumptionNorm, double OutsideTemperature, double TotalOperatingHours,
            double OperatingHours, int CurrentPowerPercentage, string UnitType, int ActiveUnitsCount, double LowerHeatingValue)
        {
            // Получаем нормы из сервиса
            var AtmosphericCoefficient = _coefficientService.GetTemperatureCoefficient(OutsideTemperature);
            var OperatingHoursCoefficient = _coefficientService.GetOperatingTimeCoefficient(TotalOperatingHours);
            var PowerCoefficient = _coefficientService.GetPowerCoefficient(UnitType, CurrentPowerPercentage);

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
                LowerHeatingValue
            );

            return gasConsumption;
        }

        private double CalculateGasConsumptionFormula(
            double OperatingHours,
            int standartPower,
            double GasConsumptionNorm,
            double AtmosphericCoefficient,
            double OperatingHoursCoefficient,
            double PowerCoefficient,
            int ActiveUnitsCount,
            double LowerHeatingValue)
        {
            double correctionK = 1.1 * AtmosphericCoefficient * OperatingHoursCoefficient * PowerCoefficient;
            double calorieK = LowerHeatingValue / 7000;
            double finalConsumption = standartPower * GasConsumptionNorm * correctionK * ActiveUnitsCount * OperatingHours * 0.001 / calorieK;

            return Math.Round(finalConsumption, 3);
        }

        //public ElectricityConsumptionData CreateCalculationData(
        //    string unitType,
        //    int activeUnitsCount,
        //    double outsideTemperature,
        //    double operatingHours,
        //    double totalOperatingHours,
        //    double unitPowerPercentage,
        //    double lowerHeatingValue)
        //{
        //    return new ElectricityConsumptionData
        //    {
        //        UnitType = unitType,
        //        ActiveUnitsCount = activeUnitsCount,
        //        OutsideTemperature = outsideTemperature,
        //        OperatingHours = operatingHours,
        //        TotalOperatingHours = totalOperatingHours,
        //        UnitPowerPercentage = Math.Clamp(unitPowerPercentage, 0, 100),
        //        LowerHeatingValue = lowerHeatingValue,
        //        Timestamp = DateTime.Now
        //    };
        //}
    }
}
