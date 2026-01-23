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

    public int? Version { get; set; }

    public string Content { get; set; } = null!;

    public string? AnswerType { get; set; }

    public int? OrderIndex { get; set; }

    public string? Dimension { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    public bool? IsActive { get; set; }

    [InverseProperty("Question")]
    public virtual ICollection<QuestionAnswer> QuestionAnswers { get; set; } = new List<QuestionAnswer>();

    [ForeignKey("TestTypeId")]
    [InverseProperty("Questions")]
    public virtual TestType TestType { get; set; } = null!;
}
