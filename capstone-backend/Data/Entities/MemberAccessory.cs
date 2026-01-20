using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class MemberAccessory
{
    [Key]
    public int Id { get; set; }

    public int? MemberId { get; set; }

    public int? AccessoryId { get; set; }

    public bool? IsEquipped { get; set; }

    public DateTime? AcquiredAt { get; set; }

    public DateTime? ExpiryAt { get; set; }

    [ForeignKey("accessory_id")]
    [InverseProperty("member_accessories")]
    public virtual Accessory? accessory { get; set; }

    [ForeignKey("member_id")]
    [InverseProperty("member_accessories")]
    public virtual member_profile? member { get; set; }
}
