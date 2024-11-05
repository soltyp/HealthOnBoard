using System.Data.SqlClient;
using System.Threading.Tasks;
using HospitalManagementAPI.Models;
using Microsoft.Extensions.Configuration;
using Dapper;
using System.Net.Http;
using System.Diagnostics;

public class DatabaseService
{
    private readonly IConfiguration _configuration;
    private readonly string _connectionString;

    public DatabaseService(IConfiguration configuration)
    {
        _configuration = configuration;
        _connectionString = configuration.GetConnectionString("HospitalManagementDB");
    }



    public async Task<User?> AuthenticateUserAsync(string pin)
    {
        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                // Sprawdzenie, czy użytkownik o podanym PIN istnieje i jest aktywny
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
                Debug.WriteLine(user == null ? "Zapytanie zwróciło null" : "Zapytanie zwróciło wynik");

                if (user != null)
                {
                    // Jeśli użytkownik istnieje, zwróć jego dane
                    Debug.WriteLine($"Pobrano z bazy danych użytkownika: {user.FirstName}, Rola: {user.Role}");
                    return user;
                }
                else
                {
                    Debug.WriteLine("Nie znaleziono użytkownika w bazie danych.");
                    return null;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Błąd podczas połączenia z bazą danych: " + ex.Message);
            return null;
        }
    }


}
