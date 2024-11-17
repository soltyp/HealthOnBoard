using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthOnBoard
{
    public class Patient
    {
        public int PatientID { get; set; }
        public string Name { get; set; } = string.Empty; // Domyślna wartość
        public int Age { get; set; }
        public int BedNumber { get; set; }
    }

}