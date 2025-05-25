using System.ComponentModel.DataAnnotations.Schema;

namespace ABCapoAPi.Data;

public class User
{
    [Column("UserID")]
    public int Id { get; set; }

    [Column("FullName")]
    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }
    public DateTime? CreatedAt { get; set; }
}
