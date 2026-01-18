using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class question
{
    [Key]
    public int id { get; set; }

    public int test_type_id { get; set; }

    public string content { get; set; } = null!;

    public string? answer_type { get; set; }

    public int? order_index { get; set; }

    public DateTime? created_at { get; set; }

    public DateTime? updated_at { get; set; }

    public bool? is_deleted { get; set; }

    public bool? is_active { get; set; }

    [InverseProperty("question")]
    public virtual ICollection<question_answer> question_answers { get; set; } = new List<question_answer>();

    [ForeignKey("test_type_id")]
    [InverseProperty("questions")]
    public virtual test_type test_type { get; set; } = null!;
}
