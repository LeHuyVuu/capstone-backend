using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Entities;

[Index("member_id", Name = "idx_blog_member")]
public partial class blog
{
    [Key]
    public int id { get; set; }

    public int member_id { get; set; }

    public string? title { get; set; }

    public string? content { get; set; }

    public int? like_count { get; set; }

    public int? comment_count { get; set; }

    public string? visibility { get; set; }

    public string? status { get; set; }

    public DateTime? created_at { get; set; }

    public DateTime? updated_at { get; set; }

    public bool? is_deleted { get; set; }

    [InverseProperty("blog")]
    public virtual ICollection<blog_like> blog_likes { get; set; } = new List<blog_like>();

    [InverseProperty("blog")]
    public virtual ICollection<comment> comments { get; set; } = new List<comment>();

    [ForeignKey("member_id")]
    [InverseProperty("blogs")]
    public virtual member_profile member { get; set; } = null!;
}
