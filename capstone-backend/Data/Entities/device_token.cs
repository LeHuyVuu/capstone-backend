using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class device_token
{
    [Key]
    public int id { get; set; }

    public int user_id { get; set; }

    public string token_hash { get; set; } = null!;

    public string? platform { get; set; }

    public DateTime? created_at { get; set; }

    public DateTime? updated_at { get; set; }

    public bool? is_deleted { get; set; }

    [ForeignKey("user_id")]
    [InverseProperty("device_tokens")]
    public virtual UserAccount user { get; set; } = null!;
}
