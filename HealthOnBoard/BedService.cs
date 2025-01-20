using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using HospitalManagementData;
using Microsoft.Extensions.Configuration;



namespace HealthOnBoard
{
    public class BedService
    {
        private readonly string _connectionString;
        

        public BedService(DatabaseService databaseService)
        {
            _connectionString = databaseService.GetConnectionString();
        }

        // Pobierz wszystkie łóżka
        public async Task<List<Bed>> GetAllBedsAsync()
        {
            var query = "SELECT BedID, BedNumber, IsOccupied, PatientID FROM Beds";
            var beds = new List<Bed>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(query, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        beds.Add(new Bed
                        {
                            BedID = reader.GetInt32(0),
                            BedNumber = reader.GetInt32(1),
                            IsOccupied = reader.GetBoolean(2),
                            PatientID = reader.IsDBNull(3) ? null : reader.GetInt32(3)
                        });
                    }
                }
            }

            return beds;
        }

        // Dodaj łóżko
        public async Task AddBedAsync(int bedNumber)
        {
            var query = "INSERT INTO Beds (BedNumber, IsOccupied, PatientID) VALUES (@BedNumber, 0, NULL)";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@BedNumber", bedNumber);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        // Przypisz pacjenta do łóżka
        public async Task<bool> AssignBedAsync(int bedNumber, int patientId)
        {
            var query = @"
            UPDATE Beds
            SET IsOccupied = 1, PatientID = @PatientID
            WHERE BedNumber = @BedNumber AND IsOccupied = 0";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@BedNumber", bedNumber);
                    command.Parameters.AddWithValue("@PatientID", patientId);

                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
        }

        // Zwolnij łóżko
        public async Task<bool> ReleaseBedAsync(int bedNumber)
        {
            var query = "UPDATE Beds SET IsOccupied = 0, PatientID = NULL WHERE BedNumber = @BedNumber";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@BedNumber", bedNumber);

                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
        }

        // Pobierz dostępne łóżka
        public async Task<List<int>> GetAvailableBedsAsync()
        {
            var query = "SELECT BedNumber FROM Beds WHERE IsOccupied = 0";
            var availableBeds = new List<int>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(query, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        availableBeds.Add(reader.GetInt32(0));
                    }
                }
            }

            return availableBeds;
        }
    }

}
