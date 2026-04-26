using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCapoAPi.Data
{
    [Table("Copies")]
    public class CopyOption
    {
        // --- هذا هو التعديل المطلوب: إضافة [Key] ---
        [Key]
        [Column("CopyID")]
        public int CopyID { get; set; }
        // ---------------------------------------------

        [Column("Name")]
        public string Name { get; set; } = string.Empty;
    }
}