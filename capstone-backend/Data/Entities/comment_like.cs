using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Entities;

public partial class comment_like
{
    [Key]
    public int id { get; set; }

    public int? comment_id { get; set; }

    public int? member_id { get; set; }

    public DateTime? created_at { get; set; }

    [ForeignKey("comment_id")]
    [InverseProperty("comment_likes")]
    public virtual comment? comment { get; set; }

    [ForeignKey("member_id")]
    [InverseProperty("comment_likes")]
    public virtual member_profile? member { get; set; }
}
