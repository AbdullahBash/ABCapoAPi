namespace ABCapoAPi.DTOs;

public class PlaceOrderDto
{
    public int? UserId { get; set; }

    // بيانات الضيف
    public string? GuestName { get; set; }
    public string? GuestPhone { get; set; }

    // بيانات العنوان
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public int CountryId { get; set; }

    public List<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();
}

// --- تعديل مهم: إضافة الحقول الجديدة هنا ---
public class OrderItemDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }

    // هذه الحقول ضرورية لتلقي البيانات من الفرونت إند
    public int? ColorId { get; set; }
    public int? SizeId { get; set; }
    public int? CopyId { get; set; }
}