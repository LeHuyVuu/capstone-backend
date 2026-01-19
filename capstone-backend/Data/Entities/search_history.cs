using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

[Index("keyword", Name = "idx_search_history_keyword")]
[Index("member_id", Name = "idx_search_history_member")]
public partial class search_history
{
    [Key]
    public int id { get; set; }

    public int? member_id { get; set; }

    public string keyword { get; set; } = null!;

    [Column(TypeName = "jsonb")]
    public string? filter_criteria { get; set; }

    public int? result_count { get; set; }

    public DateTime? searched_at { get; set; }

    public bool? is_deleted { get; set; }

    [ForeignKey("member_id")]
    [InverseProperty("search_histories")]
    public virtual member_profile? member { get; set; }
}
