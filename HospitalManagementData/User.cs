using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace HealthOnBoard
{
    public class User
    {
        [JsonPropertyName("userID")]
        public int UserID { get; set; }

        [JsonPropertyName("name")]
        public string FirstName { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("roleID")]
        public int RoleID { get; set; } // Właściwość RoleID (związana z Role)

        [JsonPropertyName("activeStatus")]
        public bool ActiveStatus { get; set; }

        [JsonPropertyName("pin")]
        public string Pin { get; set; } // Właściwość PIN

        [JsonPropertyName("safetyPin")]
        public string SafetyPIN { get; set; } // Właściwość SafetyPIN

        [JsonPropertyName("lastLogin")]
        public DateTime? LastLogin { get; set; } // Ostatnie logowanie (opcjonalne)
    }

}
