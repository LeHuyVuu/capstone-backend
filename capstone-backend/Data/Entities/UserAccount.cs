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
    public string? CitizenIdFrontUrl { get; set; }

    public string? CitizenIdBackUrl { get; set; }

    public string? BusinessLicenseUrl { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    [InverseProperty("User")]
    public virtual ICollection<DeviceToken> DeviceTokens { get; set; } = new List<DeviceToken>();

    [InverseProperty("User")]
    public virtual ICollection<MemberProfile> MemberProfiles { get; set; } = new List<MemberProfile>();

    [InverseProperty("User")]
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    [InverseProperty("User")]
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    [InverseProperty("User")]
    public virtual ICollection<VenueOwnerProfile> VenueOwnerProfiles { get; set; } = new List<VenueOwnerProfile>();

    [InverseProperty("User")]
    public virtual ICollection<Wallet> Wallets { get; set; } = new List<Wallet>();
}
