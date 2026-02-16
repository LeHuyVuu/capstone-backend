using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

[Index("TransType", "DocNo", Name = "idx_transactions_poly")]
public partial class Transaction
{
    [Key]
    public int Id { get; set; }

    [Precision(18, 2)]
    public decimal Amount { get; set; }

    public string Currency { get; set; } = null!;

    public int UserId { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public int TransType { get; set; }

    public int DocNo { get; set; }

    public string? Description { get; set; }

    public string? ExternalRefCode { get; set; }

    [MaxLength(10)]
    public string Currency { get; set; } = null!;

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
