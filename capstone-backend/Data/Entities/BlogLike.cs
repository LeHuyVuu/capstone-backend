using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class BlogLike
{
    [Key]
    public int Id { get; set; }

    public int? BlogId { get; set; }

    public int? MemberId { get; set; }

    public DateTime? CreatedAt { get; set; }

    [ForeignKey("blog_id")]
    [InverseProperty("blog_likes")]
    public virtual Blog? blog { get; set; }

    [ForeignKey("member_id")]
    [InverseProperty("blog_likes")]
    public virtual member_profile? member { get; set; }
}
