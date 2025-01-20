using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HospitalManagementData
{
    public class Bed
    {
        public int BedID { get; set; }           // Unikalny identyfikator łóżka
        public int BedNumber { get; set; }       // Numer łóżka
        public bool IsOccupied { get; set; }     // Czy łóżko jest zajęte
        public int? PatientID { get; set; }      // ID przypisanego pacjenta (null, jeśli wolne)

        public override string ToString()
        {
            return $"BedID: {BedID}, BedNumber: {BedNumber}, IsOccupied: {IsOccupied}, PatientID: {(PatientID.HasValue ? PatientID.Value.ToString() : "None")}";
        }
    }
}
