using System.ComponentModel.DataAnnotations;

namespace GasForecast.Models.DTO
{
    public class ElectricityCalculationRequestDTO
    {
        [Required(ErrorMessage = "ID ЭСН обязателен")]
        public int StationId { get; set; }

        [Required(ErrorMessage = "Температура (С) обязательна")]
        [Range(-30, 40, ErrorMessage = "Температура должна быть в диапазоне от -30°C до 40°C")]
        public double OutsideTemperature { get; set; }

        [Required(ErrorMessage = "Время работы (ч) обязательно")]
        [Range(0, 100000, ErrorMessage = "Время работы должно быть положительным")]
        public double OperatingHours { get; set; }

        [Required(ErrorMessage = "Мощность (%) агрегатов обязательна")]
        [Range(20, 100, ErrorMessage = "Мощность агрегатв должна быть от 20% до 100%")]
        public double UnitPowerPercentage { get; set; }

        [Required(ErrorMessage = "Теплота сгорания (ккал/м³) обязательна")]
        [Range(7000, 10000, ErrorMessage = "Теплота сгорания должна быть от 7000 до 10000 ккал/м³")]
        public double LowerHeatingValue { get; set; }
    }
}
