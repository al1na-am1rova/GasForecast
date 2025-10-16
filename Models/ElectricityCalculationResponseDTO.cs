namespace GasForecast.Models.DTO
{
    public class ElectricityCalculationResponseDTO
    {
        public double GasConsumption { get; set; }
        public string Unit { get; set; } = "м³";
        public DateTime CalculationTime { get; set; }
    }
}
