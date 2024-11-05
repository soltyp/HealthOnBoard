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

        [JsonPropertyName("firstName")]
        public string? FirstName { get; set; }

        [JsonPropertyName("role")]
        public string? Role { get; set; }  // Dodajemy właściwość Role jako string

        [JsonPropertyName("activeStatus")]
        public bool ActiveStatus { get; set; }
    }



}
