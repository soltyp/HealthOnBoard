using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using System;
using System.Data;

namespace HospitalManagementData
{
    public class BloodTypeHandler : SqlMapper.TypeHandler<BloodType>
    {
        public override void SetValue(IDbDataParameter parameter, BloodType value)
        {
            parameter.Value = value == null ? DBNull.Value : value.Type;
        }


        public override BloodType Parse(object value)
        {
            return BloodType.FromString(value.ToString());
        }
    }
}
