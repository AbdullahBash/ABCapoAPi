using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ABCapoAPi.Data
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }

        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        public int? BrandId { get; set; }
        public Brand? Brand { get; set; }

        public string ImageUrl { get; set; } = string.Empty;

        // --- الخصائص الجديدة المضافة (للخيارات: Color, Size, Copy) ---

        [Column("ColorID")]
        public int? ColorID { get; set; }

        [Column("SizeID")]
        public int? SizeID { get; set; }

        [Column("CopyID")]
        public int? CopyID { get; set; }

        // خصائص الربط (Navigation Properties)
        public Color? Color { get; set; }
        public Size? Size { get; set; }
        public CopyOption? Copy { get; set; }

        // --- الخصائص الجديدة المطلوبة (للترتيب والظهور) ---
        public int SortOrder { get; set; } = 0;
        public bool IsFeatured { get; set; } = false;
        public bool IsVisible { get; set; } = true;
        // ---------------------------------------------------
    }
}