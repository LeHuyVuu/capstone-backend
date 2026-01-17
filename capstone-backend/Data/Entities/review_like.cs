using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Entities;

public partial class review_like
{
    [Key]
    public int id { get; set; }

    public int? review_id { get; set; }

    public int? member_id { get; set; }

    public DateTime? created_at { get; set; }

    [ForeignKey("member_id")]
    [InverseProperty("review_likes")]
    public virtual member_profile? member { get; set; }

    [ForeignKey("review_id")]
    [InverseProperty("review_likes")]
    public virtual review? review { get; set; }
}
