namespace ABCapoAPi.DTOs;

public class OrderResponseDto
{
    public int OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string UserName { get; set; } = string.Empty;
    public List<OrderItemResponseDto> Items { get; set; } = [];
}


