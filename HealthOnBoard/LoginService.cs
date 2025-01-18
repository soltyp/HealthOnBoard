using HealthOnBoard;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Data.SqlClient;

public class LoginService
{
    private readonly string _connectionString;

    public LoginService()
    {
        // Zaktualizowany connection string z TrustServerCertificate
        _connectionString = "Server=TUF15;Database=HospitalManagement;Trusted_Connection=True;TrustServerCertificate=True;";
    }

    public async Task<User?> AuthenticateUserAsync(string pin)
    {
        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = "SELECT UserID, Name, RoleID, PIN, ActiveStatus FROM dbo.Users WHERE PIN = @PIN";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PIN", pin);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.Read())
                        {
                            return new User
                            {
                                UserID = reader.GetInt32(0),
                                FirstName = reader.GetString(1),
                                Role = reader.GetInt32(2) == 4 ? "Admin" : "User",
                                ActiveStatus = reader.GetBoolean(4)
                            };
                        }
                    }
                }
            }

            Debug.WriteLine("Nie znaleziono użytkownika z podanym PIN-em.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Błąd podczas łączenia z bazą danych: {ex.Message}");
        }

        return null;
    }

    public async Task<bool> AuthenticateSecurityPinAsync(string securityPin)
    {
        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = "SELECT COUNT(*) FROM dbo.Users WHERE SafetyPIN = @SafetyPIN";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@SafetyPIN", securityPin);

                    var count = (int)await command.ExecuteScalarAsync();
                    return count > 0;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Błąd podczas łączenia z bazą danych: {ex.Message}");
        }

        return false;
    }

    public bool IsAdmin(User user)
    {
        return user?.Role != null && user.Role.ToLower() == "admin";
    }
}