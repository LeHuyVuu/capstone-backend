using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

[Index("rank_position", Name = "idx_top_search_rank")]
[Index("keyword", Name = "top_searches_keyword_key", IsUnique = true)]
public partial class top_search
{
    [Key]
    public int id { get; set; }

    public string keyword { get; set; } = null!;

    public int? hit_count { get; set; }

    public int? rank_position { get; set; }

    public string? trend_type { get; set; }

    public DateTime? last_updated_at { get; set; }

    public bool? is_active { get; set; }
}
