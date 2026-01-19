using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class member_accessory
{
    [Key]
    public int id { get; set; }

    public int? member_id { get; set; }

    public int? accessory_id { get; set; }

    public bool? is_equipped { get; set; }

    public DateTime? acquired_at { get; set; }

    public DateTime? expiry_at { get; set; }

    [ForeignKey("accessory_id")]
    [InverseProperty("member_accessories")]
    public virtual accessory? accessory { get; set; }

    [ForeignKey("member_id")]
    [InverseProperty("member_accessories")]
    public virtual member_profile? member { get; set; }
}
