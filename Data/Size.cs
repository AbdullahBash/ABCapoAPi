using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCapoAPi.Data
{
    [Table("Sizes")]
    public class Size
    {
        [Key] // <--- أضف هذا
        [Column("SizeID")]
        public int SizeID { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}