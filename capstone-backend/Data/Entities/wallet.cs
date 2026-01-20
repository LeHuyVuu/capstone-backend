using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class Wallet
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    [Precision(18, 2)]
    public decimal? Balance { get; set; }

    public int? Points { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("user_id")]
    [InverseProperty("wallets")]
    public virtual UserAccount user { get; set; } = null!;

    [InverseProperty("wallet")]
    public virtual ICollection<WithdrawRequest> withdraw_requests { get; set; } = new List<WithdrawRequest>();
}
