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

    [ForeignKey("AccessoryId")]
    [InverseProperty("MemberAccessories")]
    public virtual Accessory? Accessory { get; set; }

    [ForeignKey("MemberId")]
    [InverseProperty("MemberAccessories")]
    public virtual MemberProfile? Member { get; set; }
}
