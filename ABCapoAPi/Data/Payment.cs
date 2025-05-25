using System.ComponentModel.DataAnnotations.Schema;

namespace ABCapoAPi.Data;

public class Payment
{
    [Column("PaymentID")]
    public int Id { get; set; }

    public int? OrderId { get; set; }
    public Order? Order { get; set; }

    public string PaymentMethod { get; set; } = string.Empty;
    public string? PaymentStatus { get; set; }
    public string? TransactionID { get; set; }

    public DateTime? CreatedAt { get; set; }
}
