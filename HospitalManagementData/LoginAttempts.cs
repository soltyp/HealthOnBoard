using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HospitalManagementData
{
    internal class LoginAttempts
    {
        public int AttemptID { get; set; }           // Unikalny identyfikator próby logowania
        public int? UserID { get; set; }            // ID użytkownika (może być null, jeśli użytkownik nie został znaleziony)
        public DateTime AttemptDate { get; set; }   // Data i czas próby logowania
        public bool Successful { get; set; }        // Czy próba była udana
        public bool Locked { get; set; }            // Czy konto było zablokowane
        public string PIN { get; set; }
        public string SecurityPin { get; set; }

        public int BedNumber {  get; set; }
    }
}
