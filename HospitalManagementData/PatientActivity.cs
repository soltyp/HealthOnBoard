using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HospitalManagementData
{
    public class PatientActivity
    {
        public int LogID { get; set; } // Unikalny identyfikator wpisu
        public int PatientID { get; set; } // Identyfikator pacjenta
        public string ActionType { get; set; } // Typ akcji (np. "Pomiar temperatury")
        public string ActionDetails { get; set; } // Szczegóły akcji
        public DateTime ActionDate { get; set; } // Data akcji
        public decimal? CurrentTemperature { get; set; } // Bieżąca temperatura, jeśli dotyczy
    }


}
