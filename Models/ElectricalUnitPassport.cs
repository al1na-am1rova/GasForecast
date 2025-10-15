namespace GasForecast.Models
{
    public class ElectricalUnitPassport
    {
        public int Id { get; set; } // id
        public string UnitType { get; set; } // тип агрегата
        public  string EngineType { get; set; } // тип двигателя поршневой или газотурбинный
        public int RatedPower { get; set; } // номинальная мощность
        public int StandartPower { get; set; } // стандарт мощности Nэсн
        public double ConsumptionNorm { get; set; } // норма расхода газа Нг
    }
}