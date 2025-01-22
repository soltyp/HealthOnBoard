using HealthOnBoard;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;

public class LoginService
{
    private readonly string _connectionString;

    public LoginService()
    {
        // Twój obecny connection string
        _connectionString = "Server=LAPTOP-72SPAJ8D;Database=HospitalManagement;Trusted_Connection=True;TrustServerCertificate=True;";
    }
    public bool IsAdmin(User user)
    {
        return user?.Role != null && user.Role.ToLower() == "admin";
    }
    public async Task<User?> AuthenticateUserAsync(string pin, string securityPin, int? bedNumber = null)
    {
        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Sprawdzanie, czy konto jest zablokowane
                if (await IsAccountLockedAsync(connection))
                {
                    Debug.WriteLine("Konto użytkownika jest zablokowane.");
                    await LogLoginAttemptAsync(null, false, true, bedNumber); // Zapisz próbę logowania zablokowanego użytkownika
                    return null;
                }

                // Sprawdzanie PIN-u użytkownika
                var query = "SELECT UserID, Name, RoleID, ActiveStatus FROM dbo.Users WHERE PIN = @PIN";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PIN", pin);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.Read())
                        {
                            var user = new User
                            {
                                UserID = reader.GetInt32(0),
                                FirstName = reader.GetString(1),
                                Role = reader.GetInt32(2) == 4 ? "Admin" : "User",
                                ActiveStatus = reader.GetBoolean(3)
                            };

                            // Loguj udane logowanie z numerem łóżka
                            await LogLoginAttemptAsync(user.UserID, true, false, bedNumber);
                            return user;
                        }
                    }
                }

                Debug.WriteLine("Nie znaleziono użytkownika z podanym PIN-em.");
                await LogLoginAttemptAsync(null, false, false, bedNumber); // Loguj nieudane logowanie
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Błąd podczas uwierzytelniania: {ex.Message}");
        }

        return null;
    }


    private async Task<bool> IsAccountLockedAsync(SqlConnection connection)
    {
        var query = "SELECT TOP 1 IsLocked FROM dbo.GlobalLoginState ORDER BY ID DESC";

        using (var command = new SqlCommand(query, connection))
        {
            var result = await command.ExecuteScalarAsync();
            return result != null && (bool)result;
        }
    }

    public async Task<bool> AuthenticateSecurityPinAsync(string securityPin)
    {
        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = "SELECT COUNT(*) FROM dbo.PIN_Bezpieczenstwa WHERE PIN = @PIN";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PIN", securityPin);
                    var count = (int)await command.ExecuteScalarAsync();
                    return count > 0;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Błąd podczas sprawdzania PIN-u bezpieczeństwa: {ex.Message}");
            return false;
        }
    }

    private async Task LogLoginAttemptAsync(int? userId, bool successful, bool locked, int? bedNumber = null)
    {
        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = @"
                INSERT INTO dbo.LoginAttempts (UserID, AttemptDate, Successful, Locked, BedNumber)
                VALUES (@UserID, @AttemptDate, @Successful, @Locked, @BedNumber)";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserID", (object?)userId ?? DBNull.Value);
                    command.Parameters.AddWithValue("@AttemptDate", DateTime.UtcNow);
                    command.Parameters.AddWithValue("@Successful", successful);
                    command.Parameters.AddWithValue("@Locked", locked);
                    command.Parameters.AddWithValue("@BedNumber", (object?)bedNumber ?? DBNull.Value);

                    await command.ExecuteNonQueryAsync();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Błąd podczas zapisywania logu logowania: {ex.Message}");
        }
    }




}

