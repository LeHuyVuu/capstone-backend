using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

[Index("token_hash", Name = "idx_refresh_token_hash")]
[Index("user_id", Name = "idx_refresh_token_user")]
public partial class refresh_token
{
    [Key]
    public int id { get; set; }

    public int user_id { get; set; }

    public string token_hash { get; set; } = null!;

    public string jwt_id { get; set; } = null!;

    public DateTime? used_at { get; set; }

    public DateTime? revoked_at { get; set; }

    public string? replaced_by_token_id { get; set; }

    public DateTime expiry_date { get; set; }

    public DateTime? created_at { get; set; }

    public DateTime? updated_at { get; set; }

    public bool? is_deleted { get; set; }

    [ForeignKey("user_id")]
    [InverseProperty("refresh_tokens")]
    public virtual user_account user { get; set; } = null!;
}
