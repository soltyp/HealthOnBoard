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
        public int PatientID { get; set; } // Klucz główny pacjenta
        public string Name { get; set; } // Imię i nazwisko pacjenta
        public int Age { get; set; } // Wiek pacjenta
        public int? BedNumber { get; set; } // Opcjonalny numer łóżka przypisanego pacjentowi
        public string CurrentTemperature { get; set; } // Aktualna temperatura pacjenta
        public string AssignedDrugs { get; set; } // Przepisane leki
        public string Notes { get; set; } // Notatki dotyczące pacjenta
        public string PESEL { get; set; } // Numer PESEL pacjenta
        public string Address { get; set; } // Adres pacjenta
        public string PhoneNumber { get; set; } // Numer telefonu pacjenta
        public string Email { get; set; } // Adres e-mail pacjenta
        public DateTime? DateOfBirth { get; set; } // Data urodzenia pacjenta
        public string Gender { get; set; } // Płeć pacjenta
        public string EmergencyContact { get; set; } // Kontakt w nagłych wypadkach
        public string Allergies { get; set; } // Alergie pacjenta
        public string ChronicDiseases { get; set; } // Choroby przewlekłe pacjenta

        // ID grupy krwi (klucz obcy do tabeli BloodTypes)
        public int? BloodTypeID { get; set; }

        // Obiekt reprezentujący grupę krwi (dla relacji z tabelą BloodTypes)
        public BloodType BloodType { get; set; }
    }




}