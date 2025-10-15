using System.ComponentModel.DataAnnotations;

namespace GasForecast.Auth
{
    public class AuthUserData
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Логин обязателен")]
        public string Login { get; set; }

        [Required(ErrorMessage = "Пароль обязателен")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Роль обязательна")]
        public string Role { get; set; }
    }
}
