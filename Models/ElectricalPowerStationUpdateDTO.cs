using System.ComponentModel.DataAnnotations;

namespace GasForecast.Models.DTO
{
    public class ElectricalPowerStationUpdateDTO
    {
        [StringLength(200, ErrorMessage = "Название не должно превышать 200 символов")]
        public string? Name { get; set; }

        [StringLength(100, ErrorMessage = "Тип агрегатов не должен превышать 100 символов")]
        public string? UnitType { get; set; }

        [Range(1, 30, ErrorMessage = "Количество агрегатов должно быть от 1 до 30")]
        public int? ActiveUnitsCount { get; set; }

        [DataType(DataType.Date)]
        [LaunchDateNotFuture(ErrorMessage = "Дата запуска не может быть позже текущей даты")]
        public DateTime? LaunchDate { get; set; }
    }
}