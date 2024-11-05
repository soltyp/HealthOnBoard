using System.Text.Json.Serialization;

namespace HospitalManagementAPI.Models
{
    public class User
    {
        [JsonPropertyName("userID")]
        public int UserID { get; set; }

        [JsonPropertyName("firstName")]
        public string? FirstName { get; set; }

        [JsonPropertyName("role")]
        public string? Role { get; set; }  // Dodajemy właściwość Role jako string

        [JsonPropertyName("activeStatus")]
        public bool ActiveStatus { get; set; }
    }

}

