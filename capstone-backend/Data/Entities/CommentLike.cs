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

    [ForeignKey("comment_id")]
    [InverseProperty("comment_likes")]
    public virtual Comment? comment { get; set; }

    [ForeignKey("member_id")]
    [InverseProperty("comment_likes")]
    public virtual member_profile? member { get; set; }
}
