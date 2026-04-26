using ABCapoAPi.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCapoAPi.Models // تأكد من namespace الصحيح لديك
{
    // 1. الكلاس الجديد الخاص بالحالات
    public class OrderStatus
    {
        [Key]
        public int StatusID { get; set; }

        [Required]
        [MaxLength(50)]
        public string StatusName { get; set; }
    }

    public class Order
    {
        [Key]
        public int Id { get; set; } // إذا كان اسم العمود في DB هو OrderID، أضف [Column("OrderID")]

        public int? UserId { get; set; }

        // العلاقة مع المستخدم
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        // --- التغيير الأهم هنا ---
        // لم نعد نستخدم string للـ Status، بل int يربط بالجدول الجديد
        [Required]
        public int StatusID { get; set; }

        // هذه الخاصية Navigation Property ستسمح لنا بجلب اسم الحالة بسهولة (order.OrderStatus.StatusName)
        [ForeignKey("StatusID")]
        public virtual OrderStatus? OrderStatus { get; set; }

        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }

        public string? GuestName { get; set; }
        public string? GuestAddress { get; set; }
        public string? GuestPhone { get; set; }

        public virtual List<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}