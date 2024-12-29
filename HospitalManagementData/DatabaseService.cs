using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Dapper;
using System.Diagnostics;
using HospitalManagementAPI;
using HospitalManagementData;
using HealthOnBoard;
using System.Data;
using System.Data.Common;


public class DatabaseService
{
    private readonly IConfiguration _configuration;
    private readonly string _connectionString;
    private readonly IDbConnection _dbConnection;

    public DatabaseService(IConfiguration configuration)
    {
        _configuration = configuration;
        _connectionString = "Data Source=LAPTOP-72SPAJ8D;Initial Catalog=HospitalManagement;Integrated Security=True;";

        if (string.IsNullOrEmpty(_connectionString))
        {
            Debug.WriteLine("Błąd: Connection string jest pusty lub niezdefiniowany.");
            throw new InvalidOperationException("Connection string is not configured.");
        }

        // Inicjalizuj _dbConnection
        _dbConnection = new SqlConnection(_connectionString);
    }


    public async Task<Patient?> GetPatientByBedNumberAsync(int bedNumber)
    {
        using (var connection = new SqlConnection(_connectionString)) // Użycie bezpośrednio zainicjalizowanego _connectionString
        {
            const string query = "SELECT * FROM Patients WHERE BedNumber = @BedNumber";
            return await connection.QueryFirstOrDefaultAsync<Patient>(query, new { BedNumber = bedNumber });
        }
    }

    public async Task<Patient?> GetPatientByIdAsync(int patientId)
    {
        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                const string query = @"
                SELECT 
                    PatientID, Name, Age, BedNumber, CurrentTemperature, AssignedDrugs, Notes, 
                    PESEL, Address, PhoneNumber, Email, DateOfBirth, Gender, EmergencyContact, 
                    BloodType, Allergies, ChronicDiseases
                FROM dbo.Patients
                WHERE PatientID = @PatientID";

                var patient = await connection.QueryFirstOrDefaultAsync<Patient>(query, new { PatientID = patientId });
                return patient;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error retrieving patient: {ex.Message}");
            throw;
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
            SELECT TOP (@Limit) 
                LogID, 
                PatientID, 
                ActionType, 
                ActionDetails, 
                ActionDate, 
                CurrentTemperature
            FROM PatientActivityLog
            WHERE PatientID = @PatientID
              AND ActionDate >= DATEADD(DAY, -3, GETDATE())
            ORDER BY ActionDate DESC";

            var activities = await connection.QueryAsync<PatientActivity>(query, new { PatientID = patientId, Limit = limit });
            return activities.ToList();
        }
    }

    public async Task<List<PatientActivity>> GetFullActivitiesAsync(int patientId, int limit = 5000)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            const string query = @"
                SELECT TOP (@Limit) PatientID, ActionType, ActionDetails, ActionDate
                FROM PatientActivityLog
                WHERE PatientID = @PatientID
                ORDER BY ActionDate DESC";


            try
            {
                Debug.WriteLine("Wykonuję zapytanie SQL w GetFullActivitiesAsync:");
                Debug.WriteLine(query);
                Debug.WriteLine($"Parametry: PatientID = {patientId}, Limit = {limit}");

                var activities = await connection.QueryAsync<PatientActivity>(query, new { PatientID = patientId, Limit = limit });
                Debug.WriteLine($"Pobrano {activities.Count()} rekordów z tabeli PatientActivityLog.");

                return activities.ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Błąd w GetFullActivitiesAsync: {ex.Message}");
                throw;
            }
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


    //public async Task<List<PatientActivity>> GetPatientHistoryAsync(int patientId)
    //{
    //    using (var connection = new SqlConnection(_connectionString))
    //    {
    //        const string query = @"
    //        SELECT *
    //        FROM PatientActivity
    //        WHERE PatientID = @PatientID
    //        ORDER BY ActionDate DESC";

    //        var activities = await connection.QueryAsync<PatientActivity>(query, new { PatientID = patientId });
    //        return activities.ToList();
    //    }
    //}



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

    public async Task<List<User>> GetUsersAsync()
    {
        var users = new List<User>();

        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = "SELECT UserID, Name, RoleID, PIN, SafetyPIN, ActiveStatus FROM dbo.Users";
                using (var command = new SqlCommand(query, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            Debug.WriteLine($"Pobieranie użytkownika: {reader.GetString(1)}"); // Dodaj debugowanie
                            users.Add(new User
                            {
                                UserID = reader.GetInt32(0),
                                FirstName = reader.GetString(1),
                                Role = reader.GetInt32(2) == 4 ? "Admin" : "User",
                                ActiveStatus = reader.GetBoolean(5)
                            });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Błąd podczas ładowania użytkowników: {ex.Message}");
        }

        return users;
    }

    public async Task SaveUserAsync(User user)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();

            if (user.UserID == 0) // Dodawanie nowego użytkownika
            {
                var query = @"
                INSERT INTO Users (Name, PIN, SafetyPIN, RoleID, ActiveStatus)
                VALUES (@Name, @Pin, @SafetyPIN, @RoleID, @ActiveStatus)";
                await connection.ExecuteAsync(query, new
                {
                    Name = user.FirstName,
                    Pin = user.Pin,
                    SafetyPIN = user.SafetyPIN,
                    RoleID = user.RoleID,
                    ActiveStatus = user.ActiveStatus
                });
            }
            else // Aktualizacja istniejącego użytkownika
            {
                var query = @"
                UPDATE Users
                SET Name = @Name,
                    PIN = @Pin,
                    SafetyPIN = @SafetyPIN,
                    RoleID = @RoleID,
                    ActiveStatus = @ActiveStatus
                WHERE UserID = @UserID";
                await connection.ExecuteAsync(query, new
                {
                    Name = user.FirstName,
                    Pin = user.Pin,
                    SafetyPIN = user.SafetyPIN,
                    RoleID = user.RoleID,
                    ActiveStatus = user.ActiveStatus,
                    UserID = user.UserID
                });
            }
        }
    }



    public async Task DeleteUserAsync(int userId)
    {
        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                string query = "DELETE FROM dbo.Users WHERE UserID = @UserID";
                await connection.ExecuteAsync(query, new { UserID = userId });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error deleting user: {ex.Message}");
            throw;
        }
    }

    public async Task<List<Role>> GetRolesAsync()
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            const string query = "SELECT RoleID, RoleName FROM Roles";
            var roles = await connection.QueryAsync<Role>(query);
            return roles.ToList();
        }
    }

    public async Task<bool> IsPinUniqueAsync(string pin, int? userId = null)
    {
        try
        {
            string query;
            object parameters;

            if (userId.HasValue)
            {
                // Sprawdzenie unikalności PIN-u, z wykluczeniem użytkownika, którego aktualnie edytujemy
                query = "SELECT COUNT(1) FROM Users WHERE PIN = @Pin AND UserID != @UserID";
                parameters = new { Pin = pin, UserID = userId.Value };
            }
            else
            {
                // Sprawdzenie unikalności PIN-u dla nowego użytkownika
                query = "SELECT COUNT(1) FROM Users WHERE PIN = @Pin";
                parameters = new { Pin = pin };
            }

            using (var connection = new SqlConnection(_connectionString))
            {
                var count = await connection.ExecuteScalarAsync<int>(query, parameters);

                // True, jeśli PIN jest unikalny lub należy do użytkownika, którego edytujemy
                return count == 0;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Błąd podczas sprawdzania unikalności PIN-u: {ex.Message}");
            throw;
        }
    }



    public async Task<User> GetUserByIdAsync(int userId)
    {
        try
        {
            var query = "SELECT UserID, Name AS FirstName, PIN, RoleID, ActiveStatus FROM Users WHERE UserID = @UserID";

            var user = await _dbConnection.QueryFirstOrDefaultAsync<User>(query, new { UserID = userId });

            if (user != null)
            {
                Debug.WriteLine($"Pobrano użytkownika: UserID={user.UserID}, Name={user.FirstName}, PIN={user.Pin}, RoleID={user.RoleID}");
            }
            else
            {
                Debug.WriteLine("Nie znaleziono użytkownika w bazie danych.");
            }

            return user;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Błąd podczas pobierania użytkownika: {ex.Message}");
            throw;
        }
    }

    public async Task<List<Patient>> GetPatientsAsync()
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            const string query = "SELECT * FROM dbo.Patients";
            return (await connection.QueryAsync<Patient>(query)).ToList();
        }
    }

    public async Task SavePatientAsync(Patient patient)
    {
        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                // Sprawdź, czy numer łóżka jest zajęty przez innego pacjenta
                var checkBedNumberQuery = "SELECT COUNT(1) FROM Patients WHERE BedNumber = @BedNumber AND PatientID != @PatientID";
                int existingBedCount = await connection.ExecuteScalarAsync<int>(checkBedNumberQuery, new { BedNumber = patient.BedNumber, PatientID = patient.PatientID });

                if (existingBedCount > 0)
                {
                    throw new InvalidOperationException($"Numer łóżka {patient.BedNumber} jest już zajęty przez innego pacjenta.");
                }

                if (patient.PatientID == 0) // Nowy pacjent
                {
                    // Pobierz najwyższe PatientID
                    var maxPatientIdQuery = "SELECT ISNULL(MAX(PatientID), 0) FROM Patients";
                    int maxPatientId = await connection.ExecuteScalarAsync<int>(maxPatientIdQuery);

                    // Ustaw PatientID
                    patient.PatientID = maxPatientId + 1;

                    // Dodaj nowego pacjenta
                    var insertQuery = @"
                    INSERT INTO Patients (
                        PatientID, Name, Age, BedNumber, CurrentTemperature, AssignedDrugs, Notes, PESEL, Address, PhoneNumber,
                        Email, DateOfBirth, Gender, EmergencyContact, BloodType, Allergies, ChronicDiseases
                    ) VALUES (
                        @PatientID, @Name, @Age, @BedNumber, @CurrentTemperature, @AssignedDrugs, @Notes, @PESEL, @Address, 
                        @PhoneNumber, @Email, @DateOfBirth, @Gender, @EmergencyContact, @BloodType, @Allergies, @ChronicDiseases
                    )";

                    await connection.ExecuteAsync(insertQuery, patient);
                }
                else // Aktualizacja istniejącego pacjenta
                {
                    // Zaktualizuj dane pacjenta
                    var updateQuery = @"
                    UPDATE Patients
                    SET Name = @Name,
                        Age = @Age,
                        BedNumber = @BedNumber,
                        CurrentTemperature = @CurrentTemperature,
                        AssignedDrugs = @AssignedDrugs,
                        Notes = @Notes,
                        PESEL = @PESEL,
                        Address = @Address,
                        PhoneNumber = @PhoneNumber,
                        Email = @Email,
                        DateOfBirth = @DateOfBirth,
                        Gender = @Gender,
                        EmergencyContact = @EmergencyContact,
                        BloodType = @BloodType,
                        Allergies = @Allergies,
                        ChronicDiseases = @ChronicDiseases
                    WHERE PatientID = @PatientID";

                    await connection.ExecuteAsync(updateQuery, patient);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving patient: {ex.Message}");
            throw;
        }
    }


    public async Task DeletePatientAsync(int patientId)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            const string query = "DELETE FROM dbo.Patients WHERE PatientID = @PatientID";
            await connection.ExecuteAsync(query, new { PatientID = patientId });
        }
    }

    public async Task AddPatientAsync(Patient patient)
    {
        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                // Sprawdź, czy numer łóżka już istnieje
                var checkBedNumberQuery = "SELECT COUNT(1) FROM Patients WHERE BedNumber = @BedNumber";
                int existingBedCount = await connection.ExecuteScalarAsync<int>(checkBedNumberQuery, new { BedNumber = patient.BedNumber });

                if (existingBedCount > 0)
                {
                    throw new InvalidOperationException($"Numer łóżka {patient.BedNumber} jest już zajęty przez innego pacjenta.");
                }

                // Pobierz najwyższe PatientID
                var maxPatientIdQuery = "SELECT ISNULL(MAX(PatientID), 0) FROM Patients";
                int maxPatientId = await connection.ExecuteScalarAsync<int>(maxPatientIdQuery);

                // Ustaw PatientID na wartość o jeden większą
                patient.PatientID = maxPatientId + 1;

                // Wstaw pacjenta
                var query = @"
                INSERT INTO Patients (
                    PatientID, Name, Age, BedNumber, CurrentTemperature, AssignedDrugs, Notes, PESEL, Address, PhoneNumber,
                    Email, DateOfBirth, Gender, EmergencyContact, BloodType, Allergies, ChronicDiseases
                ) VALUES (
                    @PatientID, @Name, @Age, @BedNumber, @CurrentTemperature, @AssignedDrugs, @Notes, @PESEL, @Address, 
                    @PhoneNumber, @Email, @DateOfBirth, @Gender, @EmergencyContact, @BloodType, @Allergies, @ChronicDiseases
                )";

                await connection.ExecuteAsync(query, patient);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error adding patient: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> UpdateCurrentTemperatureFromDetailsAsync()
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            var query = @"
        UPDATE PatientActivityLog
        SET CurrentTemperature = 
            CAST(
                SUBSTRING(
                    ActionDetails, 
                    CHARINDEX(':', ActionDetails) + 2, 
                    CHARINDEX('°', ActionDetails) - CHARINDEX(':', ActionDetails) - 2
                ) AS DECIMAL(5, 2)
            )
        WHERE ActionType = 'Pomiar temperatury'
          AND ActionDetails LIKE '%°C';";

            var rowsAffected = await connection.ExecuteAsync(query);
            return rowsAffected > 0;
        }
    }
    public async Task<bool> UpdateActivityLogTemperatureAsync(int logId, decimal temperature)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            var query = @"
        UPDATE PatientActivityLog
        SET CurrentTemperature = @CurrentTemperature
        WHERE LogID = @LogID";

            var parameters = new
            {
                CurrentTemperature = temperature,
                LogID = logId
            };

            try
            {
                int rowsAffected = await connection.ExecuteAsync(query, parameters);
                Debug.WriteLine($"UpdateActivityLogTemperatureAsync: rowsAffected={rowsAffected}, LogID={logId}");
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Błąd w UpdateActivityLogTemperatureAsync: {ex.Message}");
                return false;
            }
        }
    }

    public async Task<List<Medication>> GetMedicationsAsync()
    {
        var query = "SELECT MedicationID, Name, Unit FROM Medications";
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        var medications = await connection.QueryAsync<Medication>(query);
        return medications.ToList();
    }


}
