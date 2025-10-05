using System.ComponentModel.DataAnnotations;

namespace GasForecast.Models.DTO
{
    public class ElectricityCalculationRequest
    {
        [Required(ErrorMessage = "Тип агрегата обязателен")]
        public string UnitType { get; set; }

        [Required(ErrorMessage = "Количество агрегатов обязательно")]
        [Range(1, 100, ErrorMessage = "Количество агрегатов должно быть от 1 до 100")]
        public int ActiveUnitsCount { get; set; }

        [Required(ErrorMessage = "Температура обязательна")]
        [Range(-50, 50, ErrorMessage = "Температура должна быть в диапазоне от -50°C до 50°C")]
        public double OutsideTemperature { get; set; }

        [Required(ErrorMessage = "Время работы обязательно")]
        [Range(0, 10000, ErrorMessage = "Время работы должно быть положительным")]
        public double OperatingHours { get; set; }

        [Required(ErrorMessage = "Общая наработка обязательна")]
        [Range(0, 100000, ErrorMessage = "Общая наработка должна быть положительной")]
        public double TotalOperatingHours { get; set; }

        [Required(ErrorMessage = "Мощность агрегата обязательна")]
        [Range(0, 100, ErrorMessage = "Мощность агрегата должна быть от 0% до 100%")]
        public double UnitPowerPercentage { get; set; }

        [Required(ErrorMessage = "Теплота сгорания обязательна")]
        [Range(7000, 10000, ErrorMessage = "Теплота сгорания должна быть от 7000 до 10000 ккал/м³")]
        public double LowerHeatingValue { get; set; }
    }
}
