namespace ABCapoAPi.DTOs;

public class ProductDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string? ImageUrl { get; set; }

    // --- أضفنا هذين السطرين المفقودين ---
    public int? CategoryId { get; set; }   // لإرسال رقم التصنيف للفرونت إند
    public int? BrandId { get; set; }      // لإرسال رقم الماركة للفرونت إند
    // ----------------------------------

    public string? CategoryName { get; set; }
    public string? BrandName { get; set; }

    // --- الخصائص الجديدة المضافة سابقاً ---
    public int? ColorID { get; set; }
    public string? ColorName { get; set; }
    public string? ColorHex { get; set; }

    public int? SizeID { get; set; }
    public string? SizeName { get; set; }

    public int? CopyID { get; set; }
    public string? CopyName { get; set; }
    // ------------------------------------
}