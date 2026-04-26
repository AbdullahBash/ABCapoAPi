namespace ABCapoAPi.DTOs;

public class OrderStatusDto
{
    public int StatusID { get; set; }
    public string StatusName { get; set; }
}

public class OrderResponseDto
{
    public int OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string UserName { get; set; } = string.Empty;

    // --- إضافة هذا الجزء الجديد ---
    public OrderStatusDto? OrderStatus { get; set; }
    // ----------------------------------

    public List<OrderItemResponseDto> Items { get; set; } = new List<OrderItemResponseDto>();
}