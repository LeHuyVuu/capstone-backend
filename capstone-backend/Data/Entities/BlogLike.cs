using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class BlogLike
{
    [Key]
    public int Id { get; set; }

    public int? BlogId { get; set; }

    public int? MemberId { get; set; }

    public DateTime? CreatedAt { get; set; }

    [ForeignKey("BlogId")]
    [InverseProperty("BlogLikes")]
    public virtual Blog? Blog { get; set; }

    [ForeignKey("MemberId")]
    [InverseProperty("BlogLikes")]
    public virtual MemberProfile? Member { get; set; }
}
