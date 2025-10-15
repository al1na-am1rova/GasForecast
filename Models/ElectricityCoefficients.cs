using System.Text.Json;

namespace GasForecast.Models.ElectricityCoefficients
{
    public class ElectricityCoefficients
    {
        public TemperatureCoefficient temperatureCoefficient { get; set; }
        public OperatingTimeCoefficient operatingTimeCoefficient { get; set; }
        public PowerCoefficient powerCoefficient { get; set; }
    }

    public class TemperatureCoefficient
    {
        public string Title { get; set; }
        public string CoefficientName { get; set; }
        public List<TemperatureData> Data { get; set; }
    }

    public class TemperatureData
    {
        public int Temperature { get; set; }
        public double Coefficient { get; set; }
    }

    public class OperatingTimeCoefficient
    {
        public string Title { get; set; }
        public string CoefficientName { get; set; }
        public List<OperatingTimeData> Data { get; set; }
    }

    public class OperatingTimeData
    {
        public string OperatingHours { get; set; }
        public double Coefficient { get; set; }
    }

    public class PowerCoefficient
    {
        public string Title { get; set; }
        public string CoefficientName { get; set; }
        public List<PowerData> Data { get; set; }
    }

    public class PowerData
    {
        public string PowerRange { get; set; }
        public double Piston { get; set; }
        public double GasTurbine { get; set; }
    }
}