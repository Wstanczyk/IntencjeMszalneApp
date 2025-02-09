using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IntencjeMszalneApp.Models
{
    public class Reservation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int MassId { get; set; } // ID Mszy

        [Required]
        [StringLength(255)]
        public string Intention { get; set; } // Treść intencji

        [Required]
        public string UserId { get; set; } // ID użytkownika (Google ID)

        [ForeignKey("MassId")]
        public virtual Mass Mass { get; set; }
    }
}