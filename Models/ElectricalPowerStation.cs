namespace GasForecast.Models
{
    public class ElectricalPowerStation
    {
        public int Id { get; set; } // id
        public string Name { get; set; } //  название
        public int ActiveUnitsCount { get; set; } // кол-во агрегатов
        public string UnitType { get; set; } // тип электроагрегатов
        public DateTime LaunchDate { get; set; } // дата запуска

        // Целевая переменная  
        public double GasConsumption { get; set; } // Расход газа на электроэнергию, м³
    }
}
