namespace GasForecast.Models
{
    public class UserElectricalStation
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ElectricalStationId { get; set; }

        // Навигационные свойства
        public virtual User? User { get; set; }
        public virtual ElectricalPowerStation? ElectricalPowerStation { get; set; }
    }
}