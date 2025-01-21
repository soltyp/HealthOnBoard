using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HospitalManagementData
{
    public class BloodTypeStatisticsModel
    {
        public string BloodType { get; set; }
        public int PatientCount { get; set; }
        public string PatientName { get; set; }
        public int BedNumber { get; set; }
    }
}
