using System.Data.SqlClient;
using System.Threading.Tasks;
using HospitalManagementAPI.Models;
using Microsoft.Extensions.Configuration;
using Dapper;
using System.Diagnostics;
using HospitalManagementAPI;
using HospitalManagementData;
using HealthOnBoard;


public class DatabaseService
{
    private readonly IConfiguration _configuration;
    private readonly string _connectionString;

    public DatabaseService(IConfiguration configuration)
    {
        _configuration = configuration;
        _connectionString = "Data Source=LAPTOP-72SPAJ8D;Initial Catalog=HospitalManagement;Integrated Security=True;\r\n";



        if (string.IsNullOrEmpty(_connectionString))
        {
            Debug.WriteLine("Błąd: Connection string jest pusty lub niezdefiniowany.");
            throw new InvalidOperationException("Connection string is not configured.");
        }
    }

    public async Task<Patient?> GetPatientByBedNumberAsync(int bedNumber)
    {
        using (var connection = new SqlConnection(_connectionString)) // Użycie bezpośrednio zainicjalizowanego _connectionString
        {
            const string query = "SELECT * FROM Patients WHERE BedNumber = @BedNumber";
            return await connection.QueryFirstOrDefaultAsync<Patient>(query, new { BedNumber = bedNumber });
        }
    }


    public async Task<bool> IsLockedOutAsync()
    {
        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var state = await connection.QueryFirstOrDefaultAsync<GlobalLoginState>(
                    "SELECT TOP 1 * FROM GlobalLoginState ORDER BY ID DESC"
                );
                return state != null && state.IsLocked;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Błąd podczas sprawdzania stanu blokady: {ex.Message}");
            throw;
        }
    }

    public async Task IncrementFailedAttemptsAsync()
    {
        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                await connection.ExecuteAsync(
                    "UPDATE GlobalLoginState SET FailedAttempts = FailedAttempts + 1, IsLocked = CASE WHEN FailedAttempts + 1 >= 3 THEN 1 ELSE 0 END"
                );
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Błąd podczas inkrementacji liczby nieudanych prób: {ex.Message}");
            throw;
        }
    }

    public async Task ResetFailedAttemptsAsync()
    {
        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                await connection.ExecuteAsync(
                    "UPDATE GlobalLoginState SET FailedAttempts = 0, IsLocked = 0"
                );
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Błąd podczas resetowania liczby nieudanych prób: {ex.Message}");
            throw;
        }
    }

    public async Task<int> GetFailedLoginAttemptsAsync()
    {
        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = "SELECT TOP 1 FailedAttempts FROM GlobalLoginState ORDER BY ID DESC";
                return await connection.QueryFirstOrDefaultAsync<int>(query);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Błąd podczas pobierania liczby nieudanych prób logowania: {ex.Message}");
            throw;
        }
    }


    public async Task<User?> AuthenticateUserAsync(string pin)
    {
        if (await IsLockedOutAsync())
        {
            Debug.WriteLine("Logowanie zablokowane po 3 nieudanych próbach. Wymagany PIN bezpieczeństwa.");
            return null;
        }

        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = @"
                    SELECT 
                        u.UserID, 
                        u.Name AS FirstName, 
                        u.ActiveStatus, 
                        r.RoleName AS Role
                    FROM 
                        Users u
                    JOIN 
                        Roles r ON u.RoleID = r.RoleID
                    WHERE 
                        u.PIN = @Pin AND u.ActiveStatus = 1";

                var user = await connection.QueryFirstOrDefaultAsync<User>(query, new { Pin = pin });

                if (user != null)
                {
                    await ResetFailedAttemptsAsync();
                    Debug.WriteLine($"Pobrano z bazy danych użytkownika: {user.FirstName}, Rola: {user.Role}");
                    return user;
                }
                else
                {
                    await IncrementFailedAttemptsAsync();
                    Debug.WriteLine("Nie znaleziono użytkownika w bazie danych lub PIN niepoprawny.");
                    return null;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Błąd podczas uwierzytelniania użytkownika: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> AuthenticateSecurityPinAsync(string securityPin)
    {
        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = "SELECT TOP 1 PIN FROM PIN_Bezpieczenstwa WHERE PIN = @Pin";
                var result = await connection.QueryFirstOrDefaultAsync<string>(query, new { Pin = securityPin });

                if (!string.IsNullOrEmpty(result))
                {
                    await ResetFailedAttemptsAsync();
                    Debug.WriteLine("PIN bezpieczeństwa poprawny. Odblokowano możliwość logowania.");
                    return true;
                }
                else
                {
                    Debug.WriteLine("PIN bezpieczeństwa niepoprawny.");
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Błąd podczas uwierzytelniania PIN-u bezpieczeństwa: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> AddPatientActionAsync(int userID, int patientID, string actionType, string actionDetails, DateTime actionDate)
    {
        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                string query = @"
                INSERT INTO PatientActivityLog (UserID, PatientID, ActionType, ActionDetails, ActionDate)
                VALUES (@UserID, @PatientID, @ActionType, @ActionDetails, @ActionDate)";

                await connection.ExecuteAsync(query, new
                {
                    UserID = userID,
                    PatientID = patientID,
                    ActionType = actionType,
                    ActionDetails = actionDetails,
                    ActionDate = actionDate
                });

                return true;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Błąd podczas dodawania akcji pacjenta: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> AddPatientActivityLogAsync(int userId, int patientId, string actionType, string actionDetails, DateTime actionDate, decimal currentTemperature)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            var query = @"
        INSERT INTO PatientActivityLog (UserID, PatientID, ActionType, ActionDetails, ActionDate, CurrentTemperature)
        VALUES (@UserID, @PatientID, @ActionType, @ActionDetails, @ActionDate, @CurrentTemperature)";

            var parameters = new
            {
                UserID = userId,
                PatientID = patientId,
                ActionType = actionType,
                ActionDetails = actionDetails,
                ActionDate = actionDate,
                CurrentTemperature = currentTemperature
            };

            int rowsAffected = await connection.ExecuteAsync(query, parameters);
            return rowsAffected > 0;
        }
    }



    public async Task<List<PatientActivity>> GetRecentActivitiesAsync(int patientId, int limit = 5)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            const string query = @"
                 SELECT TOP (@Limit) ActionType, ActionDetails, ActionDate
                 FROM PatientActivityLog
                 WHERE PatientID = @PatientID
                   AND ActionDate >= DATEADD(DAY, -3, GETDATE())
                 ORDER BY ActionDate DESC";


            var activities = await connection.QueryAsync<PatientActivity>(query, new { PatientID = patientId, Limit = limit });
            return activities.ToList();
        }
    }

    public async Task<List<PatientActivity>> GetFullActivitiesAsync(int patientId, int limit = 5)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            const string query = @"
                 SELECT TOP (@Limit) ActionType, ActionDetails, ActionDate
                 FROM PatientActivityLog
                 WHERE PatientID = @PatientID
                 ORDER BY ActionDate DESC";


            var activities = await connection.QueryAsync<PatientActivity>(query, new { PatientID = patientId, Limit = limit });
            return activities.ToList();
        }
    }

    public async Task<bool> DeletePatientActionAsync(PatientActivity activity)
    {
        if (activity.PatientID <= 0)
        {
            Debug.WriteLine("Nieprawidłowy PatientID. Operacja usunięcia została przerwana.");
            return false;
        }

        try
        {
            // Logujemy otrzymane dane
            Debug.WriteLine("Rozpoczynam usuwanie czynności:");
            Debug.WriteLine($"ActionType: {activity.ActionType}");
            Debug.WriteLine($"ActionDetails: {activity.ActionDetails}");
            Debug.WriteLine($"ActionDate: {activity.ActionDate}");
            Debug.WriteLine($"PatientID: {activity.PatientID}");


            using (var connection = new SqlConnection(_connectionString))
            {
                Debug.WriteLine("Nawiązywanie połączenia z bazą danych...");

                string query = @"
                DELETE FROM PatientActivityLog
                WHERE ActionType = @ActionType 
              AND ActionDetails = @ActionDetails 
              AND ActionDate = @ActionDate
                AND PatientID = @PatientID";

                Debug.WriteLine($"Zapytanie SQL: {query}");

                // Logujemy przekazywane parametry
                Debug.WriteLine($"Parametry zapytania:");
                Debug.WriteLine($"- ActionType: {activity.ActionType}");
                Debug.WriteLine($"- ActionDetails: {activity.ActionDetails}");
                Debug.WriteLine($"- ActionDate: {activity.ActionDate}");
                Debug.WriteLine($"- PatientID: {activity.PatientID}");

                int rowsAffected = await connection.ExecuteAsync(query, new
                {
                    ActionType = activity.ActionType,
                    ActionDetails = activity.ActionDetails,
                    ActionDate = activity.ActionDate,
                    PatientID = activity.PatientID
                });

                Debug.WriteLine($"Liczba usuniętych wierszy: {rowsAffected}");

                return rowsAffected > 0;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Błąd podczas usuwania czynności: {ex.Message}");
            Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
            return false;
        }
    }

    public async Task<bool> UpdatePatientActionAsync(PatientActivity activity)
    {
        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                string query = @"
                UPDATE PatientActivityLog
                SET ActionType = @ActionType, 
                    ActionDetails = @ActionDetails
                WHERE ActionDate = @ActionDate
                  AND PatientID = @PatientID";

                int rowsAffected = await connection.ExecuteAsync(query, new
                {
                    ActionType = activity.ActionType,
                    ActionDetails = activity.ActionDetails,
                    ActionDate = activity.ActionDate,
                    PatientID = activity.PatientID
                });

                return rowsAffected > 0;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Błąd podczas aktualizacji czynności: {ex.Message}");
            return false;
        }
    }



    public async Task<Patient?> GetPatientDetailsAsync(int patientId)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            const string query = @"
            SELECT 
                PatientID, Name, Age, BedNumber, PESEL, Address, PhoneNumber, Email, 
                DateOfBirth, Gender, EmergencyContact, BloodType, Allergies, ChronicDiseases
            FROM dbo.Patients
            WHERE PatientID = @PatientID";

            return await connection.QueryFirstOrDefaultAsync<Patient>(query, new { PatientID = patientId });
        }
    }


    public async Task<List<PatientActivity>> GetPatientHistoryAsync(int patientId)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            const string query = @"
            SELECT *
            FROM PatientActivity
            WHERE PatientID = @PatientID
            ORDER BY ActionDate DESC";

            var activities = await connection.QueryAsync<PatientActivity>(query, new { PatientID = patientId });
            return activities.ToList();
        }
    }



    public async Task<double?> GetCurrentTemperatureAsync(int patientId)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            const string query = @"
            SELECT CurrentTemperature 
            FROM Patients
            WHERE PatientID = @PatientID";

            // Pobranie temperatury z tabeli Patients
            var temperature = await connection.QueryFirstOrDefaultAsync<double?>(query, new { PatientID = patientId });
            return temperature;
        }
    }

    public async Task<string?> GetAssignedDrugsAsync(int patientId)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            const string query = @"
        SELECT AssignedDrugs 
        FROM Patients
        WHERE PatientID = @PatientID";

            // Fetch the AssignedDrugs field
            var assignedDrugs = await connection.QueryFirstOrDefaultAsync<string?>(query, new { PatientID = patientId });
            return assignedDrugs;
        }
    }

    public async Task<string?> GetPatientNotesAsync(int patientId)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            const string query = @"
        SELECT Notes 
        FROM Patients
        WHERE PatientID = @PatientID";

            // Fetch the Notes field
            var notes = await connection.QueryFirstOrDefaultAsync<string?>(query, new { PatientID = patientId });
            return notes;
        }
    }

    public Dictionary<string, int> GetTemperatureStatistics()
    {
        var temperatureCounts = new Dictionary<string, int>();

        using (var connection = new SqlConnection(_connectionString))
        {
            connection.Open();

            var query = "SELECT CurrentTemperature, COUNT(*) AS Count FROM Patients GROUP BY CurrentTemperature";
            using (var command = new SqlCommand(query, connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var temperature = reader["CurrentTemperature"].ToString();
                        var count = Convert.ToInt32(reader["Count"]);
                        temperatureCounts[temperature] = count;
                    }
                }
            }
        }

        return temperatureCounts;
    }

    public async Task<bool> UpdatePatientTemperatureAsync(int patientId, decimal temperature)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            var query = "UPDATE Patients SET CurrentTemperature = @Temperature WHERE PatientID = @PatientID";

            var parameters = new
            {
                Temperature = temperature,
                PatientID = patientId
            };

            int rowsAffected = await connection.ExecuteAsync(query, parameters);
            return rowsAffected > 0;
        }
    }

    public async Task<List<(DateTime ActionDate, decimal Temperature)>> GetTemperatureLogsAsync(int patientId)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            var query = @"
            SELECT ActionDate, CurrentTemperature
            FROM PatientActivityLog
            WHERE PatientID = @PatientID AND CurrentTemperature IS NOT NULL
            ORDER BY ActionDate ASC";

            var logs = await connection.QueryAsync<(DateTime ActionDate, decimal Temperature)>(query, new { PatientID = patientId });
            return logs.ToList();
        }
    }


}
