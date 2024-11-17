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
}
