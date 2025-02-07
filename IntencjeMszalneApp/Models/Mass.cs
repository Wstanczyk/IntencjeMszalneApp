using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class Mass
{
    [Key]
    public int Id { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [Required]
    public TimeSpan Time { get; set; }

    [Required]
    public int MaxIntentions { get; set; } = 3;

    public ICollection<Intention> Intentions { get; set; } = new List<Intention>();
}
