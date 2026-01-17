using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Entities;

public partial class wallet
{
    [Key]
    public int id { get; set; }

    public int user_id { get; set; }

    [Precision(18, 2)]
    public decimal? balance { get; set; }

    public int? points { get; set; }

    public bool? is_active { get; set; }

    public DateTime? created_at { get; set; }

    public DateTime? updated_at { get; set; }

    [ForeignKey("user_id")]
    [InverseProperty("wallets")]
    public virtual user_account user { get; set; } = null!;

    [InverseProperty("wallet")]
    public virtual ICollection<withdraw_request> withdraw_requests { get; set; } = new List<withdraw_request>();
}
