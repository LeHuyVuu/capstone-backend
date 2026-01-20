using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

[Index("Keyword", Name = "idx_search_history_keyword")]
[Index("MemberId", Name = "idx_search_history_member")]
public partial class SearchHistory
{
    [Key]
    public int Id { get; set; }

    public int? MemberId { get; set; }

    public string Keyword { get; set; } = null!;

    [Column(TypeName = "jsonb")]
    public string? FilterCriteria { get; set; }

    public int? ResultCount { get; set; }

    public DateTime? SearchedAt { get; set; }

    public bool? IsDeleted { get; set; }

    [ForeignKey("MemberId")]
    [InverseProperty("SearchHistories")]
    public virtual MemberProfile? Member { get; set; }
}
