using System.Text.Json;
using GasForecast.Models.ElectricityCoefficients;

namespace GasForecast.Services
{
    public class ElectricityCoefficientsService
    {
        private readonly ElectricityCoefficients _coefficients;

        public ElectricityCoefficientsService()
        {
            // Загрузка данных из JSON файла
            var jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "ElectricityCoefficientsData.json");
            var jsonString = File.ReadAllText(jsonPath);
            _coefficients = JsonSerializer.Deserialize<ElectricityCoefficients>(jsonString);
        }

        public double GetTemperatureCoefficient(double temperature)
        {
            // Ищем точное соответствие температуры
            var exactMatch = _coefficients.temperatureCoefficient.Data
                .FirstOrDefault(x => x.Temperature == temperature);

            if (exactMatch != null)
                return exactMatch.Coefficient;

            // Если точного соответствия нет, берем ближайшее значение
            var sortedData = _coefficients.temperatureCoefficient.Data
                .OrderBy(x => x.Temperature)
                .ToList();

            // Если температура ниже минимальной
            if (temperature < sortedData.First().Temperature)
                return sortedData.First().Coefficient;

            // Если температура выше максимальной
            if (temperature > sortedData.Last().Temperature)
                return sortedData.Last().Coefficient;

            // Находим ближайшее значение температуры
            var nearest = sortedData
                .OrderBy(x => Math.Abs(x.Temperature - temperature))
                .First();

            return nearest.Coefficient;
        }

        public double GetOperatingTimeCoefficient(double operatingHours)
        {
            // Переводим в тысячи часов для сравнения
            double operatingHoursInThousands = operatingHours / 1000;

            if (operatingHoursInThousands <= 15)
                return _coefficients.operatingTimeCoefficient.Data[0].Coefficient;
            else if (operatingHoursInThousands <= 30)
                return _coefficients.operatingTimeCoefficient.Data[1].Coefficient;
            else
                return _coefficients.operatingTimeCoefficient.Data[2].Coefficient;
        }

        public double GetPowerCoefficient(string engineType, double powerPercentage)
        {
            // Определяем диапазон мощности
            string powerRange = GetPowerRange(powerPercentage);

            var powerData = _coefficients.powerCoefficient.Data
                .FirstOrDefault(x => x.PowerRange == powerRange);

            if (powerData == null)
                return 1.0;

            // Выбираем коэффициент в зависимости от типа двигателя
            return engineType.ToLower() switch
            {
                "поршневой" or "piston" => powerData.Piston,
                "газотурбинный" or "gasTurbine" => powerData.GasTurbine,
                _ => 1.0
            };
        }

        private string GetPowerRange(double powerPercentage)
        {
            return powerPercentage switch
            {
                >= 20 and <= 29 => "20-29",
                >= 30 and <= 39 => "30-39",
                >= 40 and <= 49 => "40-49",
                >= 50 and <= 59 => "50-59",
                >= 60 and <= 69 => "60-69",
                >= 70 and <= 79 => "70-79",
                >= 80 and <= 89 => "80-89",
                >= 90 and <= 99 => "90-99",
                100 => "100",
                _ => "100" // По умолчанию
            };
        }
    }
}