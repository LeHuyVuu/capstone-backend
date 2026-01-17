using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Entities;

public partial class question_answer
{
    [Key]
    public int id { get; set; }

    public int question_id { get; set; }

    public string answer_content { get; set; } = null!;

    public string? score_key { get; set; }

    public int? score_value { get; set; }

    public int? order_index { get; set; }

    public DateTime? created_at { get; set; }

    public DateTime? updated_at { get; set; }

    public bool? is_deleted { get; set; }

    public bool? is_active { get; set; }

    [ForeignKey("question_id")]
    [InverseProperty("question_answers")]
    public virtual question question { get; set; } = null!;
}
