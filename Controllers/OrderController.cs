using ABCapoAPi.Data;
using ABCapoAPi.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace ABCapoAPi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly AppDbContext _context;

    public OrderController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("place")]
    public async Task<ActionResult<Order>> PlaceOrder(PlaceOrderDto dto)
    {
        if (dto.Items.Count == 0)
            return BadRequest("Order must contain at least one item.");

        var user = await _context.Users.FindAsync(dto.UserId);
        if (user == null)
            return NotFound("User not found.");

        var order = new Order
        {
            CreatedAt = DateTime.UtcNow,
            UserId = dto.UserId,
            Items = new List<OrderItem>()
        };

        decimal totalAmount = 0;

        foreach (var item in dto.Items)
        {
            var product = await _context.Products.FindAsync(item.ProductId);
            if (product == null)
                return NotFound($"Product ID {item.ProductId} not found.");

            if (product.Quantity < item.Quantity)
                return BadRequest($"Insufficient stock for {product.Name}.");

            product.Quantity -= item.Quantity;

            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                Quantity = item.Quantity
            });

            totalAmount += product.Price * item.Quantity;
        }

        order.TotalAmount = totalAmount;

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderResponseDto>> GetOrder(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound();

        var response = new OrderResponseDto
        {
            OrderId = order.Id,
            OrderDate = order.CreatedAt ?? DateTime.MinValue,  // تعديل هنا بدل OrderDate
            TotalAmount = order.TotalAmount,
            UserName = order.User?.Name ?? string.Empty,
            Items = order.Items
                .Where(i => i != null && i.Product != null)
                .Select(i => new OrderItemResponseDto
                {
                    ProductId = i.ProductId.GetValueOrDefault(),
                    ProductName = i.Product?.Name ?? string.Empty,
                    Quantity = i.Quantity,
                    UnitPrice = i.Product?.Price ?? 0
                })
                .ToList()
        };

        return Ok(response);
    }
}
