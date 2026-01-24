using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class TestType
{
    [Key]
    public int Id { get; set; }

    public string? Code { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int? TotalQuestions { get; set; }

    public int? CurrentVersion { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    public bool? IsActive { get; set; }

    [InverseProperty("TestType")]
    public virtual ICollection<PersonalityTest> PersonalityTests { get; set; } = new List<PersonalityTest>();

    [InverseProperty("TestType")]
    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
}
