using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace capstone_backend.Data.Entities
{
    [Index("JobId", Name = "ix_date_plan_jobs_job_id")]
    public partial class DatePlanJob
    {
        [Key]
        public int Id { get; set; }

        public int DatePlanId { get; set; }

        public string JobId { get; set; }

        public string JobType { get; set; }

        [ForeignKey("DatePlanId")]
        [InverseProperty("DatePlanJobs")]
        public DatePlan DatePlan { get; set; } // Navigation Property
    }
}
