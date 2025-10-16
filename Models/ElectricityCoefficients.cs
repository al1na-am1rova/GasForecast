using System.Text.Json.Serialization;

namespace GasForecast.Models.ElectricityCoefficients
{
    public class ElectricityCoefficients
    {
        [JsonPropertyName("temperatureCoefficient")]
        public TemperatureCoefficient TemperatureCoefficient { get; set; }

        [JsonPropertyName("operatingTimeCoefficient")]
        public OperatingTimeCoefficient OperatingTimeCoefficient { get; set; }

        [JsonPropertyName("powerCoefficient")]
        public PowerCoefficient PowerCoefficient { get; set; }
    }

    public class TemperatureCoefficient
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("coefficientName")]
        public string CoefficientName { get; set; }

        [JsonPropertyName("data")]
        public List<TemperatureData> Data { get; set; }
    }

    public class TemperatureData
    {
        [JsonPropertyName("temperature")]
        public int Temperature { get; set; }

        [JsonPropertyName("coefficient")]
        public double Coefficient { get; set; }
    }

    public class OperatingTimeCoefficient
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("coefficientName")]
        public string CoefficientName { get; set; }

        [JsonPropertyName("data")]
        public List<OperatingTimeData> Data { get; set; }
    }

    public class OperatingTimeData
    {
        [JsonPropertyName("operatingHours")]
        public string OperatingHours { get; set; }

        [JsonPropertyName("coefficient")]
        public double Coefficient { get; set; }
    }

    public class PowerCoefficient
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("coefficientName")]
        public string CoefficientName { get; set; }

        [JsonPropertyName("data")]
        public List<PowerData> Data { get; set; }
    }

    public class PowerData
    {
        [JsonPropertyName("powerRange")]
        public string PowerRange { get; set; }

        [JsonPropertyName("piston")]
        public double Piston { get; set; }

        [JsonPropertyName("gasTurbine")]
        public double GasTurbine { get; set; }
    }
}