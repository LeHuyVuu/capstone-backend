using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class WithdrawRequest
{
    [Key]
    public int Id { get; set; }

    public int WalletId { get; set; }

    [Precision(18, 2)]
    public decimal? Amount { get; set; }

    [Column(TypeName = "jsonb")]
    public string? BankInfo { get; set; }

    public string? Status { get; set; }

    public string? RejectionReason { get; set; }

    public string? ProofImageUrl { get; set; }

    public DateTime? RequestedAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("WalletId")]
    [InverseProperty("WithdrawRequests")]
    public virtual Wallet Wallet { get; set; } = null!;
}
