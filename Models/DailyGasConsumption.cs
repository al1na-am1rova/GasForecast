namespace GasForecast.Models
{
    public class DailyGasConsumption
    {
        public int Id { get; set; }
        public int ElectricalStationId { get; set; }
        public DateTime Date { get; set; }
        public decimal Consumption { get; set; }

        // Навигационное свойство для связи
        public virtual ElectricalPowerStation ElectricalPowerStation { get; set; }
    }

    public class GasConsumptionCreateDto
    {
        public int ElectricalStationId { get; set; }
        public DateTime Date { get; set; }
        public decimal Consumption { get; set; }
    }

    public class GasConsumptionUpdateDto
    {
        public int Id { get; set; }
        public int ElectricalStationId { get; set; }
        public DateTime Date { get; set; }
        public decimal Consumption { get; set; }
    }
}
