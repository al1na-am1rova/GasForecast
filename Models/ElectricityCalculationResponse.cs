namespace GasForecast.Models.DTO
{
    public class ElectricityCalculationResponse
    {
        public double GasConsumption { get; set; }
        public string Unit { get; set; } = "м³";
        public DateTime CalculationTime { get; set; }
        public string CalculationId { get; set; }
    }
}
