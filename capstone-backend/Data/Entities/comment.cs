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

    public int AuthorId { get; set; }

    public int PostId { get; set; }

    public string Content { get; set; } = null!;

    public int? ParentId { get; set; }

    public int? LikeCount { get; set; } = 0;

    public int? ReplyCount { get; set; } = 0;

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    [ForeignKey("PostId")]
    [InverseProperty("Comments")]
    public virtual Post Post { get; set; } = null!;

    [InverseProperty("Comment")]
    public virtual ICollection<CommentLike> CommentLikes { get; set; } = new List<CommentLike>();

    [ForeignKey("AuthorId")]
    [InverseProperty("Comments")]
    public virtual MemberProfile Author { get; set; } = null!;

    [ForeignKey("ParentId")]
    [InverseProperty("Replies")]
    public virtual Comment? Parent { get; set; }

    [InverseProperty("Parent")]
    public virtual ICollection<Comment> Replies { get; set; } = new List<Comment>();
}
