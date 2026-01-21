using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace capstone_backend.Business.DTOs.TestType
{
    public class CreateTestTypeResquest
    {
        /// <example>multipleChoice</example>
        [Required(ErrorMessage = "TestType name is required")]
        [StringLength(100, ErrorMessage = "TestType name can not exceed 100 characters")]
        public string Name { get; set; }

        /// <example>description</example>
        [Required(ErrorMessage = "Description is required")]
        [StringLength(500, ErrorMessage = "Description can not exceed 500 characters")]
        public string Description { get; set; }

        /// <example>20</example>
        [Required(ErrorMessage = "TotalQuestions is required")]
        [Range(1, 100, ErrorMessage = "TotalQuestions must be between 1 and 100")]
        public int TotalQuestions { get; set; }
    }
}
