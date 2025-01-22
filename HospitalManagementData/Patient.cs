using HospitalManagementData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HospitalManagementData;

namespace HealthOnBoard
{
    public class Patient
    {
        public int PatientID { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public int? BedNumber { get; set; }
        public float? CurrentTemperature { get; set; }
        public string AssignedDrugs { get; set; }
        public string Notes { get; set; }
        public string PESEL { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Gender { get; set; }
        public string EmergencyContact { get; set; }
        public string Allergies { get; set; }
        public string ChronicDiseases { get; set; }

        public int? BloodTypeID { get; set; } // Identyfikator grupy krwi
        public BloodType BloodType { get; set; } // Obiekt grupy krwi
        public string PatientBloodType { get; set; } // Grupa krwi bezpośrednio z tabeli Patients
    }




}