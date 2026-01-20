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
    public virtual ICollection<DeviceToken> device_tokens { get; set; } = new List<DeviceToken>();

    [InverseProperty("user")]
    public virtual ICollection<MemberProfile> member_profiles { get; set; } = new List<MemberProfile>();

    [InverseProperty("user")]
    public virtual ICollection<Notification> notifications { get; set; } = new List<Notification>();

    [InverseProperty("user")]
    public virtual ICollection<RefreshToken> refresh_tokens { get; set; } = new List<RefreshToken>();

    [InverseProperty("user")]
    public virtual ICollection<VenueOwnerProfile> venue_owner_profiles { get; set; } = new List<VenueOwnerProfile>();

    [InverseProperty("user")]
    public virtual ICollection<wallet> wallets { get; set; } = new List<wallet>();
}
