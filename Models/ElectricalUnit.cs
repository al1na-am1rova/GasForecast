namespace GasForecast.Models
{
    public class ElectricalUnit
    {
        public int Id { get; set; } // id
        public string UnitType { get; set; } // тип электроагрегата
        public int CurrentPowerPercentage { get; set; } // текущая мощность

    }
}
