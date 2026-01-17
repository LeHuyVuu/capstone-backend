using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Entities;

public partial class blog_like
{
    [Key]
    public int id { get; set; }

    public int? blog_id { get; set; }

    public int? member_id { get; set; }

    public DateTime? created_at { get; set; }

    [ForeignKey("blog_id")]
    [InverseProperty("blog_likes")]
    public virtual blog? blog { get; set; }

    [ForeignKey("member_id")]
    [InverseProperty("blog_likes")]
    public virtual member_profile? member { get; set; }
}
