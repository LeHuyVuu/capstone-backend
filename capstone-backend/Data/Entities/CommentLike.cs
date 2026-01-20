using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class CommentLike
{
    [Key]
    public int Id { get; set; }

    public int? CommentId { get; set; }

    public int? MemberId { get; set; }

    public DateTime? CreatedAt { get; set; }

    [ForeignKey("CommentId")]
    [InverseProperty("CommentLikes")]
    public virtual Comment? Comment { get; set; }

    [ForeignKey("MemberId")]
    [InverseProperty("CommentLikes")]
    public virtual MemberProfile? Member { get; set; }
}
