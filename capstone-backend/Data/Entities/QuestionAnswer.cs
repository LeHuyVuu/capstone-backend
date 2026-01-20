using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class QuestionAnswer
{
    [Key]
    public int Id { get; set; }

    public int QuestionId { get; set; }

    public string AnswerContent { get; set; } = null!;

    public string? ScoreKey { get; set; }

    public int? ScoreValue { get; set; }

    public int? OrderIndex { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    public bool? IsActive { get; set; }

    [ForeignKey("question_id")]
    [InverseProperty("question_answers")]
    public virtual Question question { get; set; } = null!;
}
