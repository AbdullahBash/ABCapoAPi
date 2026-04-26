using System.ComponentModel.DataAnnotations;

namespace ABCapoAPi.DTOs;

public class LoginDto
{
    [Required(ErrorMessage = "Email or Username is required")]
    // نحتفظ بالاسم Email ليتطابق مع الـ Frontend، 
    // لكنه سيُستخدم كمعرف (Identifier) في الـ Controller.
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = string.Empty;
}