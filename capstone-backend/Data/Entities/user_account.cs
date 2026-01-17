using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Entities;

[Index("email", Name = "idx_user_email")]
[Index("email", Name = "user_accounts_email_key", IsUnique = true)]
public partial class user_account
{
    [Key]
    public int id { get; set; }

    public string email { get; set; } = null!;

    public string password_hash { get; set; } = null!;

    public string? display_name { get; set; }

    public string? phone_number { get; set; }

    public string? avatar_url { get; set; }

    public string? role { get; set; }

    public bool? is_active { get; set; }

    public bool? is_verified { get; set; }

    public DateTime? last_login_at { get; set; }

    public DateTime? created_at { get; set; }

    public DateTime? updated_at { get; set; }

    public bool? is_deleted { get; set; }

    [InverseProperty("user")]
    public virtual ICollection<device_token> device_tokens { get; set; } = new List<device_token>();

    [InverseProperty("user")]
    public virtual ICollection<member_profile> member_profiles { get; set; } = new List<member_profile>();

    [InverseProperty("user")]
    public virtual ICollection<notification> notifications { get; set; } = new List<notification>();

    [InverseProperty("user")]
    public virtual ICollection<refresh_token> refresh_tokens { get; set; } = new List<refresh_token>();

    [InverseProperty("user")]
    public virtual ICollection<venue_owner_profile> venue_owner_profiles { get; set; } = new List<venue_owner_profile>();

    [InverseProperty("user")]
    public virtual ICollection<wallet> wallets { get; set; } = new List<wallet>();
}
