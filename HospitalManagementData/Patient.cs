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
        public string Name { get; set; }
        public int Age { get; set; }
        public string BedNumber { get; set; }
        public string PESEL { get; set; } // PESEL pacjenta
        public string Address { get; set; } // Adres pacjenta
        public string PhoneNumber { get; set; } // Numer telefonu pacjenta
        public string Email { get; set; } // Adres e-mail pacjenta
        public DateTime? DateOfBirth { get; set; } // Data urodzenia pacjenta
        public string Gender { get; set; } // Płeć pacjenta
        public string EmergencyContact { get; set; } // Kontakt w nagłych przypadkach
        public string BloodType { get; set; } // Grupa krwi pacjenta
        public string Allergies { get; set; } // Alergie pacjenta
        public string ChronicDiseases { get; set; } // Choroby przewlekłe pacjenta
    }


}