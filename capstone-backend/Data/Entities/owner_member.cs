using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

[PrimaryKey("owner_user_id", "member_user_id")]
[Index("member_user_id", "status", Name = "idx_member_status")]
[Index("owner_user_id", "status", Name = "idx_owner_status")]
public partial class owner_member
{
    [Key]
    public int owner_user_id { get; set; }

    [Key]
    public int member_user_id { get; set; }

    [StringLength(20)]
    public string status { get; set; } = null!;

    [Column(TypeName = "timestamp without time zone")]
    public DateTime created_at { get; set; }

    [Column(TypeName = "timestamp without time zone")]
    public DateTime updated_at { get; set; }
}
