using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCapoAPi.Data;

public class Category
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    // --- الخصائص الجديدة (للترتيب والظهور) ---
    public int SortOrder { get; set; } = 0;
    public bool? IsVisible { get; set; } = true;
    // ---------------------------------------

    public List<Product> Products { get; set; } = new();
}