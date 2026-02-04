using System.Text.Json.Serialization;

namespace capstone_backend.Business.DTOs.PersonalityTest
{
    public class MbtiDetail
    {
        public string Name { get; set; }
        [JsonPropertyName("reason")]
        public string Description { get; set; }
        public string ImageUrl { get; set; }
    }
}
