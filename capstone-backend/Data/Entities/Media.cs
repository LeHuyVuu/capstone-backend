using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class Media
{
    [Key]
    public int Id { get; set; }

    public int? UploaderId { get; set; }

    public string Url { get; set; } = null!;

    public string? MediaType { get; set; }

    public string? TargetType { get; set; } = null!;

    public int? TargetId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }
}
