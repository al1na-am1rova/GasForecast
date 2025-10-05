using static GasForecast.Models.ElectricityConsumptionNorms;

namespace GasForecast.Models
{
    public class ElectricityConsumptionNorms
    {

        private readonly Dictionary<string, string> UnitTypes;
        private readonly Dictionary<string, double> AvailablePowerNorms;
        private readonly Dictionary<string, double> GasConsumptionNorms;
        private readonly Dictionary<double, double> AtmosphericCoefficients;
        private readonly Dictionary<(int min, int max), double> GasTurbinePowerCoefficients;
        private readonly Dictionary<(int min, int max), double> PistonPowerCoefficients;
        public ElectricityConsumptionNorms()
        {
            UnitTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // Газотурбинные агрегаты
                { "ПГТЭС-1500-2Г", "Газотурбинный" },
                { "ГТУ-2,5П", "Газотурбинный" },
                { "ПАЭС-2500М", "Газотурбинный" },
                { "ГТЭС-2,5", "Газотурбинный" },
                { "ЭГ-2500", "Газотурбинный" },
                { "ГТЭС-4", "Газотурбинный" },
                { "ЭГ-6000", "Газотурбинный" },
                { "БГТЭС-9,5", "Газотурбинный" },
                { "ГТЭС-12", "Газотурбинный" },
                { "ЭМ-16-25", "Газотурбинный" },
                { "ГТЭ-25У", "Газотурбинный" },
                
                // Поршневые агрегаты
                { "АСГД-500", "Поршневой" },
                { "ЭГ-500", "Поршневой" },
                { "ДГ-98М", "Поршневой" }
            };

            AvailablePowerNorms = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                // Газотурбинные агрегаты - располагаемая мощность
                { "ПГТЭС-1500-2Г", 1200 },
                { "ГТУ-2,5П", 2000 },
                { "ПАЭС-2500М", 2000 },
                { "ГТЭС-2,5", 2000 },
                { "ЭГ-2500", 2000 },
                { "ГТЭС-4", 3200 },
                { "ЭГ-6000", 4800 },
                { "БГТЭС-9,5", 7600 },
                { "ГТЭС-12", 9600 },
                { "ЭМ-16-25", 12800 },
                { "ГТЭ-25У", 23760 },
                
                // Поршневые агрегаты - располагаемая мощность
                { "АСГД-500", 400 },
                { "ЭГ-500", 400 },
                { "ДГ-98М", 800 }
            };

            GasConsumptionNorms = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                // Газотурбинные агрегаты - норма расхода газа
                { "ПГТЭС-1500-2Г", 0.61 },
                { "ГТУ-2,5П", 0.59 },
                { "ПАЭС-2500М", 0.59 },
                { "ГТЭС-2,5", 0.44 },
                { "ЭГ-2500", 0.44 },
                { "ГТЭС-4", 0.53 },
                { "ЭГ-6000", 0.41 },
                { "БГТЭС-9,5", 0.39 },
                { "ГТЭС-12", 0.37 },
                { "ЭМ-16-25", 0.36 },
                { "ГТЭ-25У", 0.33 },
                
                // Поршневые агрегаты - норма расхода газа
                { "АСГД-500", 0.30 },
                { "ЭГ-500", 0.36 },
                { "ДГ-98М", 0.39 }
            };

            AtmosphericCoefficients = new Dictionary<double, double>
            {
                { 40, 1.063 }, { 30, 1.052 }, { 25, 1.047 }, { 20, 1.042 },
                { 15, 1.037 }, { 10, 1.032 }, { 5, 1.027 }, { 0, 1.022 },
                { -5, 1.017 }, { -10, 1.012 }, { -15, 1.007 }, { -20, 1.002 },
                { -25, 0.997 }, { -30, 0.992 }
            };

            GasTurbinePowerCoefficients = new Dictionary<(int min, int max), double>
            {
                { (20, 29), 2.50 }, { (30, 39), 1.87 }, { (40, 49), 1.50 },
                { (50, 59), 1.33 }, { (60, 69), 1.20 }, { (70, 79), 1.12 },
                { (80, 89), 1.08 }, { (90, 99), 1.03 }, { (100, 100), 1.00 }
            };

            PistonPowerCoefficients = new Dictionary<(int min, int max), double>
            {
                { (20, 29), 2.30 }, { (30, 39), 1.70 }, { (40, 49), 1.45 },
                { (50, 59), 1.28 }, { (60, 69), 1.18 }, { (70, 79), 1.10 },
                { (80, 89), 1.06 }, { (90, 99), 1.03 }, { (100, 100), 1.00 }
            };

        }

        public string GetUnitType(string unitName)
        {
            if (string.IsNullOrWhiteSpace(unitName))
                return "Неизвестный";

            return UnitTypes.TryGetValue(unitName, out string type) ? type : "Неизвестный";
        }

        public double? GetAvailablePowerNorms(string unitType)
        {
            return AvailablePowerNorms.TryGetValue(unitType, out double norm) ? norm : null;
        }

        public double? GetGasConsumptionNorm(string unitType)
        {
            return GasConsumptionNorms.TryGetValue(unitType, out double norm) ? norm : null;
        }
        public double? GetAtmosphericCoefficients(double temperature)
        {
            var supportedTemps = new double[] { 40, 30, 25, 20, 15, 10, 5, 0, -5, -10, -15, -20, -25, -30 };
            double rounded_temperature = supportedTemps.OrderBy(t => Math.Abs(t - temperature)).First();
            return AtmosphericCoefficients.TryGetValue(rounded_temperature, out double coeff) ? coeff : null;
        }

        public double? GetOperatingHoursRanges(double hours)
        {
            if (hours < 15000) return 1;
            else if (hours > 30000) return 1.05;
            else return 1.02;
        }

        public double GetPowerCoefficient(string unitType, double powerPercentage)
        {
            var coefficients = unitType == "Поршневой"
                ? PistonPowerCoefficients
                : GasTurbinePowerCoefficients;

            return GetCoefficientFromRanges(coefficients, powerPercentage);
        }

        private double GetCoefficientFromRanges(Dictionary<(int min, int max), double> coefficients, double powerPercentage)
        {
            var range = coefficients.Keys.FirstOrDefault(k =>
                powerPercentage >= k.min && powerPercentage <= k.max);

            if (!range.Equals(default((int, int))))
                return coefficients[range];

            // Обработка граничных значений
            if (powerPercentage < 20)
                return coefficients[(20, 29)]; // Максимальный коэффициент

            if (powerPercentage > 100)
                return coefficients[(100, 100)]; // Минимальный коэффициент

            return 1.0; // значение по умолчанию
        }
    }
}
