using System.ComponentModel.DataAnnotations;

namespace GasForecast.Models
{
    public class ElectricalUnitPassportCreateDTO
    {
        [Required(ErrorMessage = "Тип агрегата обязателен")]
        [StringLength(100, ErrorMessage = "Тип агрегата не должен превышать 100 символов")]
        public string UnitType { get; set; }

        [Required(ErrorMessage = "Тип двигателя обязателен")]
        [RegularExpression("^(Поршневой|Газотурбинный)$", ErrorMessage = "Тип двигателя должен быть 'Поршневой' или 'Газотурбинный'")]
        public string EngineType { get; set; }

        [Required(ErrorMessage = "Мощность обязательна")]
        [Range(1, 100000, ErrorMessage = "Мощность должна быть в диапазоне 1-100000 кВт")]
        public int RatedPower { get; set; }

        [Required(ErrorMessage = "Стандартная мощность обязательна")]
        [Range(1, 100000, ErrorMessage = "Стандартная мощность должна быть в диапазоне 1-100000 кВт")]
        public int StandartPower { get; set; }

        [Required(ErrorMessage = "Норма расхода обязательна")]
        [Range(0.1, 1000.0, ErrorMessage = "Норма расхода должна быть в диапазоне 0.1-1000.0")]
        public double ConsumptionNorm { get; set; }
    }
}
