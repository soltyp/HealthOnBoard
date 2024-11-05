using HealthOnBoard;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;

public class LoginService
{
    private readonly HttpClient _httpClient;

    public LoginService()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://localhost:7229/api/") // Ustaw URL API
        };
    }

    public async Task<User?> AuthenticateUserAsync(string pin)
    {
        var content = new StringContent(JsonSerializer.Serialize(pin), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("User/Authenticate", content);

        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            Debug.WriteLine($"Odpowiedź JSON z serwera: {json}");

            return JsonSerializer.Deserialize<User>(json);
        }

        return null;
    }


}
