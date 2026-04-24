using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class Report
{
    [Key]
    public int Id { get; set; }

    public int? ReporterId { get; set; }

    public int? ReportTypeId { get; set; }

    public string? TargetType { get; set; }

    public int? TargetId { get; set; }

    [Column("evidence_snapshot", TypeName = "jsonb")]
    public string? EvidenceSnapshot { get; set; }

    public string? Reason { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    [ForeignKey("ReporterId")]
    [InverseProperty("Reports")]
    public virtual UserAccount? Reporter { get; set; }

    [ForeignKey("ReportTypeId")]
    [InverseProperty("Reports")]
    public virtual ReportType? ReportType { get; set; }
}
