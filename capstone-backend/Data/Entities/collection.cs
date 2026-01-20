using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class Collection
{
    [Key]
    public int Id { get; set; }

    public int MemberId { get; set; }

    public string? CollectionName { get; set; }

    public string? Description { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    [ForeignKey("member_id")]
    [InverseProperty("collections")]
    public virtual MemberProfile member { get; set; } = null!;

    [ForeignKey("collection_id")]
    [InverseProperty("collections")]
    public virtual ICollection<venue_location> venues { get; set; } = new List<venue_location>();
}
