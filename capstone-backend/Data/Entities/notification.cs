using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class notification
{
    [Key]
    public int id { get; set; }

    public int user_id { get; set; }

    public string title { get; set; } = null!;

    public string? message { get; set; }

    public string? type { get; set; }

    public int? reference_id { get; set; }

    public bool? is_read { get; set; }

    public DateTime? created_at { get; set; }

    [ForeignKey("user_id")]
    [InverseProperty("notifications")]
    public virtual user_account user { get; set; } = null!;
}
