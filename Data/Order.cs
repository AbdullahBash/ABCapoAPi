using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
// تأكد من استيراد Models أو Data حسب مكان وجودك
// namespace ABCapoAPi.Models 

namespace ABCapoAPi.Data // إذا كان الملف في مجلد Data
{
    // 1. تعريف الكلاس الجديد للجدول المنفصل
    public class OrderStatus
    {
        [Key]
        public int StatusID { get; set; }

        [Required]
        [MaxLength(50)]
        public string StatusName { get; set; }
    }

    // [Table("Orders")] 
    public class Order
    {
        // 1. الـ Primary Key
        [Key]
        [Column("OrderID")]
        public int Id { get; set; }

        // 2. ربط المستخدم
        [Column("UserID")]
        public int? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        // 3. التاريخ والمجموع (كما هي)
        public DateTime CreatedAt { get; set; }
        public decimal TotalAmount { get; set; }

        // ----------------------------------------------------------
        // --- التغيير الجديد والهام هنا (بدلاً من string Status) ---
        // ----------------------------------------------------------

        // أ) المفتاح الأجنبي (رقم الحالة)
        [Required]
        [Column("StatusID")] // ليتوافق مع العمود الجديد في قاعدة البيانات
        public int StatusID { get; set; }

        // ب) خاصية التنقل (للوصول لاسم الحالة)
        // هذا السطر سيزيل الخطأ الذي ظهر لك في AppDbContext
        [ForeignKey("StatusID")]
        public virtual OrderStatus? OrderStatus { get; set; }
        // ----------------------------------------------------------

        // 5. عناصر الطلب
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();

        // 6. بيانات الضيف
        public string? GuestName { get; set; }
        public string? GuestAddress { get; set; }
        public string? GuestPhone { get; set; }
    }
}