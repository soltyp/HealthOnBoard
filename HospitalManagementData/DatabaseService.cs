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
using System.Text;


public class DatabaseService
{
    private readonly IConfiguration _configuration;
    private readonly string _connectionString;
    private readonly IDbConnection _dbConnection;

    public DatabaseService(IConfiguration configuration)
    {
        _configuration = configuration;
        _connectionString = "Data Source=TUF15;Initial Catalog=HospitalManagement;Integrated Security=True;TrustServerCertificate=TRUE;";
        SqlMapper.AddTypeHandler(new BloodTypeHandler());

        try
        {
            if (string.IsNullOrEmpty(_connectionString))
            {
                Debug.WriteLine("Błąd: Connection string jest pusty lub niezdefiniowany.");
                throw new InvalidOperationException("Connection string is not configured.");
            }

            // Próba utworzenia połączenia z bazą danych
            _dbConnection = new SqlConnection(_connectionString);
            _dbConnection.Open(); // Przetestowanie połączenia podczas inicjalizacji

            Debug.WriteLine("Połączenie z bazą danych zostało pomyślnie nawiązane.");
        }
        catch (SqlException sqlEx)
        {
            // Log szczegółów błędu SQL
            Debug.WriteLine($"SQL Error: {sqlEx.Message}");
            Debug.WriteLine($"SQL Error Code: {sqlEx.Number}");
            Debug.WriteLine($"SQL Server: {sqlEx.Server}");
            Debug.WriteLine($"Stack Trace: {sqlEx.StackTrace}");
            throw; // Ponowne zgłoszenie wyjątku, jeśli potrzebne
        }   
        catch (Exception ex)
        {
            // Log innych wyjątków
            Debug.WriteLine($"General Error: {ex.Message}");
            Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
            throw; // Ponowne zgłoszenie wyjątku, jeśli potrzebne
        }
    }

    public async Task<List<LoginAttempt>> GetLoginAttemptsAsync(DateTime? filterDate = null, string filterUser = null)
    {
        const string baseQuery = @"
        SELECT 
            la.AttemptID, 
            la.UserID, 
            la.AttemptDate, 
            la.Successful, 
            ISNULL(u.Name, 'Nieznany użytkownik') AS UserName, 
            ISNULL(r.RoleName, 'Brak roli') AS RoleName, 
            ISNULL(la.BedNumber, 0) AS BedNumber,
            ISNULL(p.Name, 'Brak pacjenta') AS PatientName
        FROM [HospitalManagement].[dbo].[LoginAttempts] la
        LEFT JOIN [HospitalManagement].[dbo].[Users] u ON la.UserID = u.UserID
        LEFT JOIN [HospitalManagement].[dbo].[Roles] r ON u.RoleID = r.RoleID
        LEFT JOIN [HospitalManagement].[dbo].[Patients] p ON la.BedNumber = p.BedNumber
        WHERE ISNULL(u.Name, '') <> 'admin'"; 

    var queryBuilder = new StringBuilder(baseQuery);

        // Dodaj filtr dla daty
        if (filterDate.HasValue)
        {
            queryBuilder.Append(" AND CAST(la.AttemptDate AS DATE) = @FilterDate");
        }

        // Dodaj filtr dla użytkownika (operator LIKE dla częściowych dopasowań)
        if (!string.IsNullOrEmpty(filterUser))
        {
            queryBuilder.Append(" AND u.Name LIKE @FilterUser");
        }

        queryBuilder.Append(" ORDER BY la.AttemptDate DESC");

        using (var connection = new SqlConnection(_connectionString))
        {
            return (await connection.QueryAsync<LoginAttempt>(queryBuilder.ToString(), new
            {
                FilterDate = filterDate?.Date,
                FilterUser = $"%{filterUser}%" // Użyj % dla dopasowań częściowych
            })).ToList();
        }
    }

    public async Task<List<BedStatisticsModel>> GetBedStatisticsAsync()
    {
        const string query = @"
        SELECT 
            b.BedNumber,
            ISNULL(p.Name, 'Łóżko wolne') AS PatientName
        FROM [HospitalManagement].[dbo].[Beds] b
        LEFT JOIN [HospitalManagement].[dbo].[Patients] p ON b.BedNumber = p.BedNumber
        ORDER BY b.BedNumber";

        using (var connection = new SqlConnection(_connectionString))
        {
            return (await connection.QueryAsync<BedStatisticsModel>(query)).ToList();
        }
    }

    public async Task<List<GenderStatisticsModel>> GetGenderStatisticsAsync()
    {
        try
        {
            const string query = @"
            SELECT 
                Name,
                CASE 
                    WHEN Gender = 'Male' THEN 'Mężczyzna'
                    WHEN Gender = 'Female' THEN 'Kobieta'
                    ELSE 'Nieznana' 
                END AS Gender
            FROM [HospitalManagement].[dbo].[Patients]";

            using (var connection = new SqlConnection(_connectionString))
            {
                Debug.WriteLine("Łączenie z bazą danych w GetGenderStatisticsAsync...");
                var result = await connection.QueryAsync<GenderStatisticsModel>(query);
                Debug.WriteLine($"Pobrano {result.Count()} rekordów dotyczących płci.");
                return result.ToList();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Błąd w GetGenderStatisticsAsync: {ex.Message}");
            throw;
        }
    }


    public string GetConnectionString()
    {
        return _connectionString;
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
                p.PatientID, 
                p.Name, 
                p.Age, 
                p.BedNumber, 
                p.CurrentTemperature, 
                p.AssignedDrugs, 
                p.Notes, 
                p.PESEL, 
                p.Address, 
                p.PhoneNumber, 
                p.Email, 
                p.DateOfBirth, 
                p.Gender, 
                p.EmergencyContact, 
                p.Allergies, 
                p.ChronicDiseases, 
                b.BloodTypeID, 
                b.Type AS BloodTypeName
            FROM Patients p
            LEFT JOIN BloodTypes b ON p.BloodTypeID = b.BloodTypeID
            WHERE p.PatientID = @PatientID";

                var result = await connection.QueryAsync<Patient, BloodType, Patient>(
                    query,
                    (patient, bloodType) =>
                    {
                        patient.BloodType = bloodType; // Powiąż obiekt BloodType z Patient
                        return patient;
                    },
                    new { PatientID = patientId },
                    splitOn: "BloodTypeID" // Określ, gdzie zaczynają się dane BloodType
                );

                return result.FirstOrDefault();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Błąd w GetPatientByIdAsync: {ex.Message}");
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

    public async Task<List<PatientActivity>> GetFullActivitiesAsync(int patientId)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            string query = @"SELECT LogID, PatientID, ActionType, ActionDetails, CurrentTemperature, ActionDate
                         FROM PatientActivityLog
                         WHERE PatientID = @PatientID";

            return (await connection.QueryAsync<PatientActivity>(query, new { PatientID = patientId })).ToList();
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

    public async Task<decimal?> GetTemperatureByLogIdAsync(int logId)
    {
        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                const string query = "SELECT  FROM PatientActivityLog WHERE LogID = @LogID";

                // Wykonanie zapytania z parametrem logId
                var result = await connection.QueryFirstOrDefaultAsync<decimal?>(query, new { LogID = logId });

                Debug.WriteLine(result.HasValue
                    ? $"Znaleziono temperaturę: {result.Value}"
                    : "Nie znaleziono temperatury dla podanego LogID");

                return result;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Błąd w GetTemperatureByLogIdAsync: {ex.Message}");
            throw;
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

    public async Task<bool> UpdatePatientTemperatureAsync(int patientId, decimal newTemperature)
    {
        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = @"
                UPDATE Patients
                SET CurrentTemperature = @newTemperature
                WHERE PatientID = @patientId";

                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@newTemperature", newTemperature);
                command.Parameters.AddWithValue("@patientId", patientId);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                Debug.WriteLine($"Rows affected: {rowsAffected}");
                return rowsAffected > 0; // Zwraca true, jeśli zmieniono co najmniej jeden wiersz
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Błąd podczas aktualizacji temperatury pacjenta: {ex.Message}");
            return false;
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
    public async Task<BloodType?> GetBloodTypeByIdAsync(int bloodTypeId)
    {
        try
        {
            const string query = @"
            SELECT [BloodTypeID], [Type]
            FROM [HospitalManagement].[dbo].[BloodTypes]
            WHERE [BloodTypeID] = @BloodTypeID";

            using (var connection = new SqlConnection(_connectionString))
            {
                var result = await connection.QueryFirstOrDefaultAsync<BloodType>(query, new { BloodTypeID = bloodTypeId });
                return result; // Zwraca grupę krwi lub null, jeśli nie znaleziono
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Błąd w GetBloodTypeByIdAsync: {ex.Message}");
            return null;
        }
    }
    public async Task<List<BloodTypeStatisticsModel>> GetBloodTypeStatisticsAsync()
    {
        const string query = @"
    SELECT 
    bt.Type AS BloodType, 
    COUNT(p.PatientID) AS PatientCount,
    p.Name AS PatientName,
    p.BedNumber
FROM [HospitalManagement].[dbo].[Patients] p
LEFT JOIN [HospitalManagement].[dbo].[BloodTypes] bt ON p.BloodTypeID = bt.BloodTypeID
GROUP BY bt.Type, p.Name, p.BedNumber
";

        using (var connection = new SqlConnection(_connectionString))
        {
            try
            {
                // Otwórz połączenie
                await connection.OpenAsync();

                // Wykonaj zapytanie
                var result = await connection.QueryAsync<BloodTypeStatisticsModel>(query);

                // Sprawdź, czy wynik jest pusty
                if (result == null || !result.Any())
                {
                    Debug.WriteLine("Brak danych dla grup krwi.");
                    return new List<BloodTypeStatisticsModel>();  // Zwróć pustą listę
                }

                // Zwróć wyniki
                return result.ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Błąd podczas pobierania danych grup krwi: {ex.Message}");
                throw;
            }
        }
    }


    public async Task<List<Patient>> GetPatientsAsync()
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            const string query = @"
            SELECT 
                p.PatientID, p.Name, p.Age, p.BedNumber, 
                p.PESEL, p.Address, p.PhoneNumber, p.Email, 
                p.DateOfBirth, p.Gender, p.EmergencyContact, 
                p.Allergies, p.ChronicDiseases, p.Notes, 
                b.BloodTypeID, b.Type AS BloodTypeName
            FROM Patients p
            LEFT JOIN BloodTypes b ON p.BloodTypeID = b.BloodTypeID";

            var patients = await connection.QueryAsync<Patient, BloodType, Patient>(
                query,
                (patient, bloodType) =>
                {
                    patient.BloodType = bloodType;
                    return patient;
                },
                splitOn: "BloodTypeID");

            return patients.ToList();
        }
    }


    public async Task SavePatientAsync(Patient patient)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            const string query = @"
        UPDATE Patients
        SET 
            Name = @Name,
            Age = @Age,
            BedNumber = @BedNumber,
            BloodType = @BloodType, -- Przypisanie stringa BloodType
            PESEL = @PESEL,
            Address = @Address,
            PhoneNumber = @PhoneNumber,
            Email = @Email,
            DateOfBirth = @DateOfBirth,
            Gender = @Gender,
            EmergencyContact = @EmergencyContact,
            Allergies = @Allergies,
            ChronicDiseases = @ChronicDiseases,
            Notes = @Notes
        WHERE PatientID = @PatientID";

            await connection.ExecuteAsync(query, new
            {
                patient.Name,
                patient.Age,
                patient.BedNumber,
                BloodType = patient.BloodType, // Zapisujemy stringa
                patient.PESEL,
                patient.Address,
                patient.PhoneNumber,
                patient.Email,
                patient.DateOfBirth,
                patient.Gender,
                patient.EmergencyContact,
                patient.Allergies,
                patient.ChronicDiseases,
                patient.Notes,
                patient.PatientID
            });
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
        const string getMaxIdQuery = "SELECT ISNULL(MAX(PatientID), 0) + 1 FROM Patients;";
        const string insertQuery = @"
        INSERT INTO Patients (PatientID, Name, Age, BedNumber, CurrentTemperature, AssignedDrugs, Notes, 
                              PESEL, Address, PhoneNumber, Email, DateOfBirth, Gender, 
                              EmergencyContact, BloodType, Allergies, ChronicDiseases)
        VALUES (@PatientID, @Name, @Age, @BedNumber, @CurrentTemperature, @AssignedDrugs, @Notes, 
                @PESEL, @Address, @PhoneNumber, @Email, @DateOfBirth, @Gender, 
                @EmergencyContact, @BloodType, @Allergies, @ChronicDiseases);";

        using (var connection = new SqlConnection(_connectionString))
        {
            // Pobierz największy PatientID z bazy danych i zwiększ o 1
            int newPatientId = await connection.QuerySingleAsync<int>(getMaxIdQuery);
            patient.PatientID = newPatientId;

            // Wstaw pacjenta z przypisanym PatientID
            await connection.ExecuteAsync(insertQuery, new
            {
                PatientID = patient.PatientID,
                patient.Name,
                patient.Age,
                patient.BedNumber,
                patient.CurrentTemperature,
                patient.AssignedDrugs,
                patient.Notes,
                patient.PESEL,
                patient.Address,
                patient.PhoneNumber,
                patient.Email,
                patient.DateOfBirth,
                patient.Gender,
                patient.EmergencyContact,
                BloodType = patient.BloodType?.Type ?? (object)DBNull.Value,
                patient.Allergies,
                patient.ChronicDiseases
            });
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

    public async Task<bool> UpdateActivityLogAsync(int logId, string actionType, string actionDetails, decimal? newTemperature = null)
    {
        if (logId <= 0)
        {
            Debug.WriteLine("LogID jest nieprawidłowy. Aktualizacja przerwana.");
            return false;
        }

        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync(); // Otwórz połączenie

            using (var transaction = await connection.BeginTransactionAsync())
            {
                try
                {
                    // 1. Aktualizacja ActionDetails i (opcjonalnie) CurrentTemperature w PatientActivityLog
                    string updateLogQuery = @"
                UPDATE PatientActivityLog
                SET 
                    ActionDetails = @ActionDetails,
                    CurrentTemperature = CASE WHEN @CurrentTemperature IS NOT NULL THEN @CurrentTemperature ELSE CurrentTemperature END
                WHERE LogID = @LogID";

                    // Formatowanie ActionDetails na podstawie typu akcji
                    string formattedActionDetails = actionDetails;

                    if (actionType == "Pomiar temperatury" && newTemperature.HasValue)
                    {
                        formattedActionDetails = $"Zmierzono temperaturę: {newTemperature:F1}°C";
                    }
                    else if (actionType == "Podanie leków")
                    {
                        formattedActionDetails = $"{actionDetails}";
                    }
                    else if (actionType == "Dodanie wyników badań")
                    {
                        formattedActionDetails = $"Dodano wyniki: {actionDetails}";
                    }

                    Debug.WriteLine($"Executing SQL for LogID: {logId}");
                    Debug.WriteLine($"Formatted ActionDetails: {formattedActionDetails}");
                    Debug.WriteLine($"NewTemperature: {newTemperature?.ToString("F1") ?? "null"}");

                    var logParameters = new
                    {
                        ActionDetails = formattedActionDetails,
                        CurrentTemperature = newTemperature,
                        LogID = logId
                    };

                    int rowsAffected = await connection.ExecuteAsync(updateLogQuery, logParameters, transaction);

                    Debug.WriteLine($"Rows affected in PatientActivityLog: {rowsAffected}");
                    if (rowsAffected == 0)
                    {
                        Debug.WriteLine("Nie udało się zaktualizować PatientActivityLog.");
                        await transaction.RollbackAsync();
                        return false;
                    }

                    // 2. Pobranie PatientID dla LogID
                    string getPatientIdQuery = @"
                SELECT PatientID
                FROM PatientActivityLog
                WHERE LogID = @LogID";

                    int patientId = await connection.ExecuteScalarAsync<int>(getPatientIdQuery, new { LogID = logId }, transaction);

                    Debug.WriteLine($"Fetched PatientID: {patientId}");
                    if (patientId <= 0)
                    {
                        Debug.WriteLine("Nie znaleziono PatientID dla podanego LogID.");
                        await transaction.RollbackAsync();
                        return false;
                    }

                    // 3. Sprawdzenie, czy LogID jest największy dla PatientID i ActionType = 'Pomiar temperatury'
                    if (actionType == "Pomiar temperatury" && newTemperature.HasValue)
                    {
                        string isLatestLogQuery = @"
                    SELECT CASE 
                        WHEN MAX(LogID) = @LogID THEN 1
                        ELSE 0
                    END AS IsLatestLog
                    FROM PatientActivityLog
                    WHERE PatientID = @PatientID
                      AND ActionType = 'Pomiar temperatury'";

                        Debug.WriteLine($"SQL to check latest LogID: {isLatestLogQuery} | LogID: {logId}, PatientID: {patientId}");

                        int isLatestLogResult = await connection.ExecuteScalarAsync<int>(
                            isLatestLogQuery,
                            new { LogID = logId, PatientID = patientId },
                            transaction
                        );

                        bool isLatestLog = isLatestLogResult == 1; // Konwersja 1 -> true, 0 -> false

                        Debug.WriteLine($"Czy aktualizowany LogID jest największy? {isLatestLog}");

                        // 4. Jeśli bieżący LogID jest największy, aktualizuj tabelę Patients
                        if (isLatestLog)
                        {
                            string updatePatientQuery = @"
                        UPDATE Patients
                        SET CurrentTemperature = @CurrentTemperature
                        WHERE PatientID = @PatientID";

                            Debug.WriteLine($"SQL to update Patients table: {updatePatientQuery}");
                            Debug.WriteLine($"Updating CurrentTemperature: {newTemperature?.ToString("F1") ?? "null"} for PatientID: {patientId}");

                            var patientParameters = new { CurrentTemperature = newTemperature, PatientID = patientId };
                            int patientRowsAffected = await connection.ExecuteAsync(updatePatientQuery, patientParameters, transaction);

                            Debug.WriteLine($"Rows affected in Patients table: {patientRowsAffected}");
                        }
                        else
                        {
                            Debug.WriteLine("Aktualizowany LogID nie jest największym dla PatientID i ActionType = 'Pomiar temperatury'.");
                        }
                    }

                    // 5. Zatwierdzenie transakcji
                    await transaction.CommitAsync();
                    Debug.WriteLine("Aktualizacja zakończona sukcesem.");
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Wystąpił błąd podczas aktualizacji: {ex.Message}");
                    Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                    await transaction.RollbackAsync();
                    return false;
                }
            }
        }
    }

    public async Task<bool> UpdateActivityLogTemperatureAsync(int logId, decimal newTemperature)
    {
        if (logId <= 0)
        {
            Debug.WriteLine("LogID jest nieprawidłowy. Aktualizacja przerwana.");
            return false;
        }

        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync(); // Otwórz połączenie

            using (var transaction = await connection.BeginTransactionAsync())
            {
                try
                {
                    Debug.WriteLine("Rozpoczynam aktualizację PatientActivityLog...");

                    // 1. Aktualizacja CurrentTemperature i ActionDetails w PatientActivityLog
                    string updateLogQuery = @"
                UPDATE PatientActivityLog
                SET 
                    CurrentTemperature = @CurrentTemperature,
                    ActionDetails = CONCAT('Zmierzono temperaturę: ', @CurrentTemperature, '°C')
                WHERE LogID = @LogID";

                    Debug.WriteLine($"SQL do aktualizacji logu: {updateLogQuery} | LogID: {logId}, NewTemperature: {newTemperature}");

                    var logParameters = new { CurrentTemperature = newTemperature, LogID = logId };
                    int rowsAffected = await connection.ExecuteAsync(updateLogQuery, logParameters, transaction);

                    Debug.WriteLine($"Wiersze zaktualizowane w PatientActivityLog: {rowsAffected}");

                    if (rowsAffected == 0)
                    {
                        Debug.WriteLine("Nie udało się zaktualizować PatientActivityLog. Rolback transakcji.");
                        await transaction.RollbackAsync();
                        return false;
                    }

                    // 2. Pobranie PatientID dla LogID
                    string getPatientIdQuery = @"
                SELECT PatientID
                FROM PatientActivityLog
                WHERE LogID = @LogID";

                    Debug.WriteLine($"SQL do pobrania PatientID: {getPatientIdQuery} | LogID: {logId}");

                    int patientId = await connection.ExecuteScalarAsync<int>(getPatientIdQuery, new { LogID = logId }, transaction);

                    Debug.WriteLine($"Pobrane PatientID: {patientId}");

                    if (patientId <= 0)
                    {
                        Debug.WriteLine("Nie znaleziono PatientID dla podanego LogID. Rolback transakcji.");
                        await transaction.RollbackAsync();
                        return false;
                    }

                    // 3. Sprawdzenie, czy LogID jest największy dla PatientID i ActionType = 'Pomiar temperatury'
                    string isLatestLogQuery = @"
SELECT CASE 
    WHEN MAX(LogID) = @LogID THEN 1
    ELSE 0
END AS IsLatestLog
FROM PatientActivityLog
WHERE PatientID = @PatientID
  AND ActionType = 'Pomiar temperatury'";

                    Debug.WriteLine($"SQL do sprawdzania największego LogID: {isLatestLogQuery} | LogID: {logId}, PatientID: {patientId}");

                    // Pobieramy wartość int (1 lub 0) i konwertujemy ją na bool
                    int isLatestLogResult = await connection.ExecuteScalarAsync<int>(
                        isLatestLogQuery,
                        new { LogID = logId, PatientID = patientId },
                        transaction
                    );

                    bool isLatestLog = isLatestLogResult == 1; // Konwersja 1 -> true, 0 -> false

                    Debug.WriteLine($"Czy aktualizowany LogID jest największy? {isLatestLog}");


                    Debug.WriteLine($"Czy aktualizowany LogID jest największy? {isLatestLog}");

                    // 4. Jeśli bieżący LogID jest największy, aktualizuj tabelę Patients
                    if (isLatestLog)
                    {
                        string updatePatientQuery = @"
                    UPDATE Patients
                    SET CurrentTemperature = @CurrentTemperature
                    WHERE PatientID = @PatientID";

                        Debug.WriteLine($"SQL do aktualizacji tabeli Patients: {updatePatientQuery} | PatientID: {patientId}, NewTemperature: {newTemperature}");

                        var patientParameters = new { CurrentTemperature = newTemperature, PatientID = patientId };
                        int patientRowsAffected = await connection.ExecuteAsync(updatePatientQuery, patientParameters, transaction);

                        Debug.WriteLine($"Wiersze zaktualizowane w Patients: {patientRowsAffected}");
                    }
                    else
                    {
                        Debug.WriteLine("Aktualizowany LogID nie jest największym dla PatientID i ActionType = 'Pomiar temperatury'.");
                    }

                    // 5. Zatwierdzenie transakcji
                    await transaction.CommitAsync();
                    Debug.WriteLine("Aktualizacja zakończona sukcesem.");
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Wystąpił błąd podczas aktualizacji: {ex.Message}");
                    Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                    await transaction.RollbackAsync();
                    return false;
                }
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

    public async Task<bool> UpdateActivityDetailsAsync(PatientActivity activity)
    {
        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                string query = @"
            UPDATE PatientActivityLog
            SET ActionType = @ActionType, 
                ActionDetails = @ActionDetails
            WHERE LogID = @LogID";

                // Wykonanie zapytania z parametrami
                int rowsAffected = await connection.ExecuteAsync(query, new
                {
                    ActionType = activity.ActionType,
                    ActionDetails = activity.ActionDetails,
                    LogID = activity.LogID
                });

                // Zwróć true, jeśli aktualizacja powiodła się
                return rowsAffected > 0;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Błąd w UpdateActivityDetailsAsync: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> IsLatestActivityLogAsync(int patientId, int logId)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            const string query = @"
            SELECT CASE 
                WHEN LogID = @LogID THEN 1 
                ELSE 0 
            END AS IsLatest
            FROM PatientActivityLog
            WHERE PatientID = @PatientID
            ORDER BY ActionDate DESC
            OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY";

            return await connection.QuerySingleOrDefaultAsync<bool>(query, new { PatientID = patientId, LogID = logId });
        }
    }

   

}
