using System;
using System.Collections.Generic;
namespace GasForecast.Models
{
    public class ElectricityConsumptionData
    {
        public int Id { get; set; }
        //public string GeneratorId { get; set; } // Идентификатор генератора
        public DateTime Timestamp { get; set; }

        // Целевая переменная  
        public double GasConsumption { get; set; } // Расход газа на электроэнергию, м³

        // Признаки 
        public string UnitType { get; set; } // тип агрегата
        public int ActiveUnitsCount { get; set; } // кол-во агрегатов
        public double OutsideTemperature { get; set; } // темп. наруж. воздуха С
        public double OperatingHours { get; set; } // время работы за расчетный период, ч
        public double TotalOperatingHours { get; set; } // наработка агрегата с начала эксплуатации, ч
        public double UnitPowerPercentage { get; set; } // мощность агрегата, %
        public double LowerHeatingValue { get; set; }  // низшая теплота сгорания топлива, ккал/м3

    }

}
