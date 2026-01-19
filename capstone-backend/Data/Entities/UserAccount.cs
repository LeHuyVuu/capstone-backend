using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

[Index("Email", Name = "idx_user_email")]
[Index("Email", Name = "user_accounts_email_key", IsUnique = true)]
public partial class UserAccount
{
    [Key]
    public int Id { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? DisplayName { get; set; }

    public string? PhoneNumber { get; set; }

    public string? AvatarUrl { get; set; }

    public string? Role { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsVerified { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

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
