using HospitalManagementData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace HealthOnBoard
{
    public class BloodService
    {
        private readonly string _connectionString;

        public BloodService(DatabaseService databaseService)
        {
            _connectionString = databaseService.GetConnectionString();
        }

        public async Task<List<BloodType>> GetAllBloodTypesAsync()
        {
            var query = "SELECT BloodTypeID, Type FROM BloodTypes";
            var bloodTypes = new List<BloodType>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(query, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        bloodTypes.Add(new BloodType
                        {
                            BloodTypeID = reader.GetInt32(0),
                            Type = reader.GetString(1)
                        });
                    }
                }
            }

            return bloodTypes;
        }
    }
}
