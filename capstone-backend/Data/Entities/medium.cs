using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Entities;

public partial class medium
{
    [Key]
    public int id { get; set; }

    public int? uploader_id { get; set; }

    public string url { get; set; } = null!;

    public string? media_type { get; set; }

    public string target_type { get; set; } = null!;

    public int target_id { get; set; }

    public DateTime? created_at { get; set; }

    public DateTime? updated_at { get; set; }

    public bool? is_deleted { get; set; }
}
