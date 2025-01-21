using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HospitalManagementData
{
    public class LoginAttempt
    {
        public int AttemptID { get; set; }
        public int? UserID { get; set; }
        public string UserName { get; set; }
        public string RoleName { get; set; }
        public DateTime AttemptDate { get; set; }
        public bool Successful { get; set; }
        public int BedNumber { get; set; }
        public string PatientName { get; set; }
    }
}
