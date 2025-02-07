using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Intention
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int MassId { get; set; }

    [ForeignKey("MassId")]
    public Mass Mass { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey("UserId")]
    public User User { get; set; }

    [Required]
    public string IntentionText { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
