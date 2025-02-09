using System.ComponentModel.DataAnnotations;

public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string GoogleId { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    public string Name { get; set; }

    [Required]
    public string Role { get; set; } = "User"; // Domyślnie zwykły użytkownik
}