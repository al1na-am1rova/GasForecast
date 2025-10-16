// Models/DTO/CreateStationRequest.cs
using System.ComponentModel.DataAnnotations;

namespace GasForecast.Models.DTO
{
    public class PowerStationRequestDTO
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
        public DateTime LaunchDate { get; set; }
    }
}