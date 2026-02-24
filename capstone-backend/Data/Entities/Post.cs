using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

[Index("CreatedAt", "Id", IsDescending = new[] { true, true }, Name = "idx_posts_feed")]
public partial class Post
{
    [Key]
    public int Id { get; set; }

    public int AuthorId { get; set; }

    public string? Content { get; set; }

    [Column(TypeName = "jsonb")]
    public List<string> MediaPayload { get; set; } = new();

    public string? LocationName { get; set; }

    public int? LikeCount { get; set; } = 0;

    public int? CommentCount { get; set; } = 0;

    public string? Visibility { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    [InverseProperty("Post")]
    public virtual ICollection<PostLike> PostLikes { get; set; } = new List<PostLike>();

    [InverseProperty("Post")]
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    [ForeignKey("AuthorId")]
    [InverseProperty("Posts")]
    public virtual MemberProfile Member { get; set; } = null!;
}
