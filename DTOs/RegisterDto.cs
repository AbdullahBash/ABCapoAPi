using System.ComponentModel.DataAnnotations;

namespace ABCapoAPi.DTOs;

public class RegisterDto
{
    [Required(ErrorMessage = "Name is required")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    public string Password { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    // --- الحقول الجديدة للعنوان ---
    public string? Address { get; set; }
    public string? City { get; set; }
    public int? CountryId { get; set; }
}