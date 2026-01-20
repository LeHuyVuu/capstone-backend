using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class personality_test
{
    [Key]
    public int id { get; set; }

    public int member_id { get; set; }

    public int test_type_id { get; set; }

    public string? result_code { get; set; }

    [Column(TypeName = "jsonb")]
    public string? result_data { get; set; }

    public string? status { get; set; }

    public DateTime? taken_at { get; set; }

    public DateTime? created_at { get; set; }

    public DateTime? updated_at { get; set; }

    public bool? is_deleted { get; set; }

    [ForeignKey("member_id")]
    [InverseProperty("personality_tests")]
    public virtual MemberProfile member { get; set; } = null!;

    [ForeignKey("test_type_id")]
    [InverseProperty("personality_tests")]
    public virtual test_type test_type { get; set; } = null!;
}
