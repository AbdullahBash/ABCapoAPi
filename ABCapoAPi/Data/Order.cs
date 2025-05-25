using System.ComponentModel.DataAnnotations.Schema;

namespace ABCapoAPi.Data;

public class Order
{
    [Column("OrderID")]
    public int Id { get; set; }

    public DateTime? CreatedAt { get; set; }
    public decimal TotalAmount { get; set; }

    public string? OrderStatus { get; set; }

    public int? UserId { get; set; }
    public User? User { get; set; }

    public List<OrderItem> Items { get; set; } = new();
}
