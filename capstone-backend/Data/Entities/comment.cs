using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class comment
{
    [Key]
    public int id { get; set; }

    public int member_id { get; set; }

    public int? blog_id { get; set; }

    public string? content { get; set; }

    public int? parent_comment_id { get; set; }

    public int? like_count { get; set; }

    public int? comment_count { get; set; }

    public string? status { get; set; }

    public DateTime? created_at { get; set; }

    public DateTime? updated_at { get; set; }

    public bool? is_deleted { get; set; }

    [ForeignKey("blog_id")]
    [InverseProperty("comments")]
    public virtual Blog? blog { get; set; }

    [InverseProperty("comment")]
    public virtual ICollection<comment_like> comment_likes { get; set; } = new List<comment_like>();

    [ForeignKey("member_id")]
    [InverseProperty("comments")]
    public virtual member_profile member { get; set; } = null!;
}
