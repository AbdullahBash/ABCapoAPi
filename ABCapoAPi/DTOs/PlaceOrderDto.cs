namespace ABCapoAPi.DTOs;

public class PlaceOrderDto
{
    public int UserId { get; set; }
    public List<OrderItemDto> Items { get; set; } = [];
}

public class OrderItemDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}
