using System.ComponentModel.DataAnnotations.Schema;

namespace ABCapoAPi.Data;

public class OrderItem
{
    [Column("OrderItemID")]
    public int Id { get; set; }

    public int? OrderId { get; set; }
    public Order? Order { get; set; }

    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    public int Quantity { get; set; }

    public decimal Price { get; set; }
}
