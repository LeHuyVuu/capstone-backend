using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class special_event
{
    [Key]
    public int id { get; set; }

    public string? event_name { get; set; }

    public string? description { get; set; }

    public DateTime? start_date { get; set; }

    public DateTime? end_date { get; set; }

    public DateTime? created_at { get; set; }

    public DateTime? updated_at { get; set; }

    public bool? is_deleted { get; set; }
}
