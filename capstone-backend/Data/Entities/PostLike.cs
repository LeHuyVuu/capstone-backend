using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

[Index("PostId", "MemberId", IsUnique = true)]
public partial class PostLike
{
    [Key]
    public int Id { get; set; }

    public int PostId { get; set; }

    public int MemberId { get; set; }

    public DateTime? CreatedAt { get; set; }

    [ForeignKey("PostId")]
    [InverseProperty("PostLikes")]
    public virtual Post Post { get; set; }

    [ForeignKey("MemberId")]
    [InverseProperty("PostLikes")]
    public virtual MemberProfile Member { get; set; }
}
