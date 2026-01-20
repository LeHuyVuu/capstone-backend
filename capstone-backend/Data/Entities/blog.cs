using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

[Index("MemberId", Name = "idx_blog_member")]
public partial class Blog
{
    [Key]
    public int Id { get; set; }

    public int MemberId { get; set; }

    public string? Title { get; set; }

    public string? Content { get; set; }

    public int? LikeCount { get; set; }

    public int? CommentCount { get; set; }

    public string? Visibility { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    [InverseProperty("Blog")]
    public virtual ICollection<BlogLike> BlogLikes { get; set; } = new List<BlogLike>();

    [InverseProperty("Blog")]
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    [ForeignKey("MemberId")]
    [InverseProperty("Blogs")]
    public virtual MemberProfile Member { get; set; } = null!;
}
