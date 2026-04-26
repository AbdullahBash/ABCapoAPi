using ABCapoAPi.Data;
using ABCapoAPi.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Linq; // تأكد من وجود هذا الـ using

namespace ABCapoAPi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrderController : ControllerBase
{
    private readonly AppDbContext _context;

    public OrderController(AppDbContext context)
    {
        _context = context;
    }

    // 1. إنشاء طلب جديد (مع حفظ الخيارات: Color, Size, Copy)
    [AllowAnonymous]
    [HttpPost("place")]
    public async Task<ActionResult<Order>> PlaceOrder(PlaceOrderDto dto)
    {
        if (dto.Items == null || dto.Items.Count == 0)
        {
            return BadRequest("Order must contain at least one item.");
        }

        // --- منطق التحقق من المستخدم ---
        User? linkedUser = null;

        if (dto.UserId.HasValue && dto.UserId.Value > 0)
        {
            linkedUser = await _context.Users.FindAsync(dto.UserId.Value);
            if (linkedUser == null)
            {
                return NotFound("User not found.");
            }
        }
        else if (string.IsNullOrEmpty(dto.GuestName))
        {
            return BadRequest("Guest name is required.");
        }

        // التحقق من العنوان
        if (string.IsNullOrEmpty(dto.Address) || string.IsNullOrEmpty(dto.City))
        {
            return BadRequest("Address and City are required.");
        }

        // جلب اسم الدولة
        var country = await _context.Countries.FindAsync(dto.CountryId);
        if (country == null) return BadRequest("Invalid Country ID.");

        string fullAddress = $"{dto.Address}, {dto.City}, {country.Name}";

        // تحديث بيانات المستخدم إذا كان مسجلاً
        if (linkedUser != null)
        {
            linkedUser.Address = dto.Address;
            linkedUser.City = dto.City;
            linkedUser.CountryID = dto.CountryId;
            _context.Entry(linkedUser).State = EntityState.Modified;
        }

        var order = new Order
        {
            CreatedAt = DateTime.UtcNow,
            UserId = linkedUser?.Id,
            Items = new List<OrderItem>(),
            GuestName = linkedUser == null ? dto.GuestName : null,
            GuestPhone = linkedUser == null ? dto.GuestPhone : linkedUser.PhoneNumber,
            GuestAddress = fullAddress,
            StatusID = 1
        };

        decimal totalAmount = 0;

        foreach (var item in dto.Items)
        {
            var product = await _context.Products.FindAsync(item.ProductId);
            if (product == null)
            {
                return NotFound($"Product ID {item.ProductId} not found.");
            }

            if (product.Quantity < item.Quantity)
            {
                return BadRequest($"Insufficient stock for {product.Name}.");
            }

            // خصم من المخزون
            product.Quantity -= item.Quantity;

            // --- حفظ الخيارات في OrderItem ---
            order.Items.Add(new OrderItem
            {
                ProductID = product.Id,
                Quantity = item.Quantity,
                Price = product.Price,
                ColorID = item.ColorId,
                SizeID = item.SizeId,
                CopyID = item.CopyId
            });
            // ----------------------------------

            totalAmount += product.Price * item.Quantity;
        }

        order.TotalAmount = totalAmount;

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
    }

    // 2. جلب طلبات المستخدم الحالي
    [HttpGet("my-orders")]
    public async Task<ActionResult<IEnumerable<object>>> GetMyOrders()
    {
        try
        {
            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not found in token.");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            var orders = await _context.Orders
                .Where(o => o.UserId == user.Id)
                .Include(o => o.OrderStatus)
                .Include(o => o.Items)
                    .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new
                {
                    o.Id,
                    OrderDate = o.CreatedAt,
                    o.TotalAmount,
                    Status = o.OrderStatus.StatusName,
                    ShippingAddress = o.GuestAddress ?? "N/A",
                    ItemsCount = o.Items.Count,
                    Items = o.Items.Select(oi => new
                    {
                        oi.Quantity,
                        oi.Price,
                        ProductName = oi.Product.Name
                    }).ToList()
                })
                .ToListAsync();

            return Ok(orders);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error fetching orders", error = ex.Message });
        }
    }

    // 3. جلب جميع الطلبات (للأدمن - مع دعم الترقيم Paging)
    [HttpGet]
    public async Task<ActionResult<object>> GetAllOrders(int pageNumber = 1, int pageSize = 10)
    {
        // 1. حساب العدد الكلي للطلبات (ضروري لحساب عدد الصفحات في الفرونت إند)
        var totalCount = await _context.Orders.CountAsync();

        // 2. جلب البيانات مع تطبيق الترقيم
        var orders = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderStatus)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((pageNumber - 1) * pageSize) // تخطي السجلات بناءً على رقم الصفحة
            .Take(pageSize)                   // جلب عدد محدد من السجلات
            .Select(o => new
            {
                o.Id,
                o.CreatedAt,
                o.TotalAmount,
                User = new { o.User.Name, o.User.PhoneNumber },
                OrderStatus = new { o.OrderStatus.StatusID, o.OrderStatus.StatusName },
                ItemsCount = o.Items.Count,
                Items = o.Items.Select(i => new {
                    i.ProductID,
                    i.Product.Name,
                    i.Product.ImageUrl,
                    i.Price,
                    i.Quantity
                }).ToList()
            })
            .ToListAsync();

        // 3. إرجاع كائن يحتوي على البيانات + العدد الكلي
        return Ok(new
        {
            TotalCount = totalCount,
            Data = orders
        });
    }

    // 4. جلب تفاصيل طلب محدد (ID) - هام جداً للـ Admin View
    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetOrder(int id)
    {
        var order = await _context.Orders
            .Include(o => o.OrderStatus)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .ThenInclude(p => p.Color)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .ThenInclude(p => p.Size)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .ThenInclude(p => p.Copy)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound();
        }

        string customerName = order.User?.Name ?? order.GuestName ?? "Guest";
        string customerPhone = order.User?.PhoneNumber ?? order.GuestPhone ?? "N/A";

        var response = new
        {
            id = order.Id,
            createdAt = order.CreatedAt,
            totalAmount = order.TotalAmount,

            user = order.User != null ? new
            {
                order.User.Name,
                order.User.PhoneNumber,
                order.User.Address,
                order.User.City,
                order.User.CountryID
            } : null,
            guestName = order.GuestName,
            guestPhone = order.GuestPhone,
            guestAddress = order.GuestAddress,

            orderStatus = new
            {
                statusID = order.StatusID,
                statusName = order.OrderStatus?.StatusName ?? "Pending"
            },

            items = order.Items
                .Where(i => i != null && i.Product != null)
                .Select(i => new
                {
                    productId = i.ProductID,
                    productName = i.Product.Name,
                    quantity = i.Quantity,
                    price = i.Price,
                    imageUrl = i.Product.ImageUrl,
                    colorName = i.Product.Color?.Name,
                    sizeName = i.Product.Size?.Name,
                    copyName = i.Product.Copy?.Name
                })
                .ToList()
        };

        return Ok(response);
    }

    // 5. تحديث الحالة
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateStatusDto dto)
    {
        var order = await _context.Orders
            .Include(o => o.OrderStatus)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound($"Order with ID {id} not found.");
        }

        var statusEntity = await _context.OrderStatuses
            .FirstOrDefaultAsync(s => s.StatusName.ToLower() == dto.Status.ToLower());

        if (statusEntity == null)
        {
            return BadRequest($"Invalid status name: {dto.Status}");
        }

        order.StatusID = statusEntity.StatusID;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }

        // منطق الواتساب
        string whatsappLink = null;
        string phoneToUse = order.User?.PhoneNumber ?? order.GuestPhone;

        if ((dto.Status.Equals("Shipped", StringComparison.OrdinalIgnoreCase) ||
             dto.Status.Equals("Delivered", StringComparison.OrdinalIgnoreCase)) &&
             !string.IsNullOrEmpty(phoneToUse))
        {
            var cleanPhone = phoneToUse.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");

            string message = "";
            if (dto.Status.Equals("Shipped", StringComparison.OrdinalIgnoreCase))
            {
                message = $"Hello! Your order #{order.Id} has been shipped and is on its way to you. Thank you for shopping with us!";
            }
            else
            {
                message = $"Hello! Your order #{order.Id} has been delivered successfully. We hope you enjoy your purchase. Thank you!";
            }

            var encodedMessage = Uri.EscapeDataString(message);
            whatsappLink = $"https://wa.me/{cleanPhone}?text={encodedMessage}";
        }

        if (!string.IsNullOrEmpty(whatsappLink))
        {
            return Ok(new { success = true, message = "Status updated", whatsappLink = whatsappLink });
        }

        return Ok(new { success = true, message = "Status updated successfully" });
    }

    // 6. حذف طلب
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOrder(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound($"Order with ID {id} not found.");
        }

        try
        {
            if (order.Items != null && order.Items.Count > 0)
            {
                _context.OrderItems.RemoveRange(order.Items);
            }

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // DTO Class for Update Status
    public class UpdateStatusDto
    {
        public string Status { get; set; }
    }
}