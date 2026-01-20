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

    [ForeignKey("BlogId")]
    [InverseProperty("Comments")]
    public virtual Blog? Blog { get; set; }

    [InverseProperty("Comment")]
    public virtual ICollection<CommentLike> CommentLikes { get; set; } = new List<CommentLike>();

    [ForeignKey("MemberId")]
    [InverseProperty("Comments")]
    public virtual MemberProfile Member { get; set; } = null!;
}
