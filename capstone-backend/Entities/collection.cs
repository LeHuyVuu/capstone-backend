using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Entities;

public partial class collection
{
    [Key]
    public int id { get; set; }

    public int member_id { get; set; }

    public string? collection_name { get; set; }

    public string? description { get; set; }

    public string? status { get; set; }

    public DateTime? created_at { get; set; }

    public DateTime? updated_at { get; set; }

    public bool? is_deleted { get; set; }

    [ForeignKey("member_id")]
    [InverseProperty("collections")]
    public virtual member_profile member { get; set; } = null!;

    [ForeignKey("collection_id")]
    [InverseProperty("collections")]
    public virtual ICollection<venue_location> venues { get; set; } = new List<venue_location>();
}
