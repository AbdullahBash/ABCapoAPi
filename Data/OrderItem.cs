using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCapoAPi.Data;

[Table("OrderItems")]
public class OrderItem
{
    [Key]
    [Column("OrderItemID")]
    public int OrderItemID { get; set; }

    [Column("OrderID")]
    public int? OrderID { get; set; }
    public Order? Order { get; set; }

    [Column("ProductID")]
    public int? ProductID { get; set; }
    public Product? Product { get; set; }

    [Column("Quantity")]
    public int Quantity { get; set; }

    [Column("Price")]
    public decimal Price { get; set; }

    // --- الحقول المفقودة (مهمة جداً) ---
    [Column("ColorID")]
    public int? ColorID { get; set; }
    public Color? Color { get; set; }

    [Column("SizeID")]
    public int? SizeID { get; set; }
    public Size? Size { get; set; }

    [Column("CopyID")]
    public int? CopyID { get; set; }
    public CopyOption? Copy { get; set; }
    // ------------------------------------
}