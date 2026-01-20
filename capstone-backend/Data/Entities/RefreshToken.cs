using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

[Index("TokenHash", Name = "idx_refresh_token_hash")]
[Index("UserId", Name = "idx_refresh_token_user")]
public partial class RefreshToken
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    public string TokenHash { get; set; } = null!;

    public string JwtId { get; set; } = null!;

    public DateTime? UsedAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public string? ReplacedByTokenId { get; set; }

    public DateTime ExpiryDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    [ForeignKey("user_id")]
    [InverseProperty("refresh_tokens")]
    public virtual UserAccount user { get; set; } = null!;
}
