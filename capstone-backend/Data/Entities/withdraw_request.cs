using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class withdraw_request
{
    [Key]
    public int id { get; set; }

    public int wallet_id { get; set; }

    [Precision(18, 2)]
    public decimal? amount { get; set; }

    [Column(TypeName = "jsonb")]
    public string? bank_info { get; set; }

    public string? status { get; set; }

    public string? rejection_reason { get; set; }

    public string? proof_image_url { get; set; }

    public DateTime? requested_at { get; set; }

    public DateTime? created_at { get; set; }

    public DateTime? updated_at { get; set; }

    [ForeignKey("wallet_id")]
    [InverseProperty("withdraw_requests")]
    public virtual Wallet wallet { get; set; } = null!;
}
