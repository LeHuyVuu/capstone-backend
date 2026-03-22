using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.SystemConfig
{
    public class UpdateSystemConfigRequest
    {
        /// <example>VENUE_COMMISSION_PERCENT</example>
        [Required]
        public string ConfigKey { get; set; } = null!;

        /// <summary>
        /// Truyền string
        /// </summary>
        /// <example>10</example>
        [Required]
        public string ConfigValue { get; set; } = null!;
    }
}
