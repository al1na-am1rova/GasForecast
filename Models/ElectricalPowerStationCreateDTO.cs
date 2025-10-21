using System.ComponentModel.DataAnnotations;

namespace GasForecast.Models.DTO
{
    public class ElectricalPowerStationCreateDTO
    {
        [Required(ErrorMessage = "Название ЭСН обязательно")]
        [StringLength(200, ErrorMessage = "Название не должно превышать 200 символов")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Тип агрегатов обязателен")]
        [StringLength(100, ErrorMessage = "Тип агрегатов не должен превышать 100 символов")]
        public string UnitType { get; set; }

        [Required(ErrorMessage = "Количество агрегатов обязательно")]
        [Range(1, 30, ErrorMessage = "Количество агрегатов должно быть от 1 до 30")]
        public int ActiveUnitsCount { get; set; }

        [Required(ErrorMessage = "Дата запуска обязательна")]
        [DataType(DataType.Date)]
        [LaunchDateNotFuture(ErrorMessage = "Дата запуска не может быть позже текущей даты")]
        public DateTime LaunchDate { get; set; }
    }
}