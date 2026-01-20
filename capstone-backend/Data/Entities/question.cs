using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class Question
{
    [Key]
    public int Id { get; set; }

    public int TestTypeId { get; set; }

    public string Content { get; set; } = null!;

    public string? AnswerType { get; set; }

    public int? OrderIndex { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    public bool? IsActive { get; set; }

    [InverseProperty("question")]
    public virtual ICollection<question_answer> question_answers { get; set; } = new List<question_answer>();

    [ForeignKey("test_type_id")]
    [InverseProperty("questions")]
    public virtual test_type test_type { get; set; } = null!;
}
