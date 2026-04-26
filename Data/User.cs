using System.ComponentModel.DataAnnotations.Schema;
// تمت إزالة using System.Diagnostics.Metrics; لأنها غير مستخدمة هنا

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

    public bool IsAdmin { get; set; } = false;

    // --- إضافة الأعمدة للعنوان مع توضيح اسم العمود في الـ DB ---
    [Column("Address")]
    public string? Address { get; set; }

    [Column("City")]
    public string? City { get; set; }

    [Column("CountryID")]
    public int? CountryID { get; set; }

    // علاقة مع جدول الدول (Navigation Property)
    public Country? Country { get; set; }
}