using System.ComponentModel.DataAnnotations.Schema;

namespace capstone_backend.Business.DTOs.PersonalityTest
{
    public class PersonalityTestResponse
    {
        public int Id { get; set; }
        public int TestTypeId { get; set; }
        public string? ResultCode { get; set; }
        public string? Status { get; set; }
        public DateTime? TakenAt { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
