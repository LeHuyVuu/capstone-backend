using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

[Index("trans_type", "doc_no", Name = "idx_transactions_poly")]
public partial class transaction
{
    [Key]
    public int id { get; set; }

    [Precision(18, 2)]
    public decimal amount { get; set; }

    public int user_id { get; set; }

    public string payment_method { get; set; } = null!;

    public int trans_type { get; set; }

    public int doc_no { get; set; }

    public string? description { get; set; }

    public string? external_ref_code { get; set; }

    public string? status { get; set; }

    public DateTime? created_at { get; set; }

    public DateTime? updated_at { get; set; }
}
