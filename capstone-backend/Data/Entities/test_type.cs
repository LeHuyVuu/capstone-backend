using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class test_type
{
    [Key]
    public int id { get; set; }

    public string name { get; set; } = null!;

    public string? description { get; set; }

    public int? total_questions { get; set; }

    public DateTime? created_at { get; set; }

    public DateTime? updated_at { get; set; }

    public bool? is_deleted { get; set; }

    public bool? is_active { get; set; }

    [InverseProperty("test_type")]
    public virtual ICollection<PersonalityTest> personality_tests { get; set; } = new List<PersonalityTest>();

    [InverseProperty("test_type")]
    public virtual ICollection<Question> questions { get; set; } = new List<Question>();
}
