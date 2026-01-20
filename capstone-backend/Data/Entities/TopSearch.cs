using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

[Index("RankPosition", Name = "idx_top_search_rank")]
[Index("Keyword", Name = "top_searches_keyword_key", IsUnique = true)]
public partial class TopSearch
{
    [Key]
    public int Id { get; set; }

    public string Keyword { get; set; } = null!;

    public int? HitCount { get; set; }

    public int? RankPosition { get; set; }

    public string? TrendType { get; set; }

    public DateTime? LastUpdatedAt { get; set; }

    public bool? IsActive { get; set; }
}
