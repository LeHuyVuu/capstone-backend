using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class Comment
{
    [Key]
    public int Id { get; set; }

    public int MemberId { get; set; }

    public int? BlogId { get; set; }

    public string? Content { get; set; }

    public int? ParentCommentId { get; set; }

    public int? LikeCount { get; set; }

    public int? CommentCount { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    [ForeignKey("blog_id")]
    [InverseProperty("comments")]
    public virtual Blog? blog { get; set; }

    [InverseProperty("comment")]
    public virtual ICollection<CommentLike> comment_likes { get; set; } = new List<CommentLike>();

    [ForeignKey("member_id")]
    [InverseProperty("comments")]
    public virtual member_profile member { get; set; } = null!;
}
