using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HealthOnBoard;

namespace HospitalManagementData
{
    public class BloodType
    {
        public int BloodTypeID { get; set; }
        public string Type { get; set; }

        public override string ToString()
        {
            return Type; 
        }

    }
}
