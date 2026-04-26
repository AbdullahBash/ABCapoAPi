using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCapoAPi.Data
{
    [Table("Colors")]
    public class Color
    {
        [Key] // <--- أضف هذا
        [Column("ColorID")]
        public int ColorID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Hex { get; set; }
    }
}