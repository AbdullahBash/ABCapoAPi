using ABCapoAPi.Data;
using ABCapoAPi.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace ABCapoAPi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class OrderController(AppDbContext _context) : ControllerBase
{
    [HttpPost("place")]
    public async Task<ActionResult<Order>> PlaceOrder(PlaceOrderDto dto)
    {
        if (dto.Items.Count == 0)
            return BadRequest("Order must contain at least one item.");

        var user = await _context.Users.FindAsync(dto.UserId);
        if (user is null)
            return NotFound("User not found.");

        var order = new Order
        {
            OrderDate = DateTime.UtcNow,
            UserId = dto.UserId,
            Items = []
        };

        decimal totalAmount = 0;

        foreach (var item in dto.Items)
        {
            var product = await _context.Products.FindAsync(item.ProductId);
            if (product is null)
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

        if (order is null)
            return NotFound();

        var response = new OrderResponseDto
        {
            OrderId = order.Id,
            OrderDate = order.OrderDate,
            TotalAmount = order.TotalAmount,
            UserName = order.User?.Name ?? string.Empty,
            Items =
            [
                .. order.Items
                    .Where(i => i != null && i.Product != null)
                    .Select(i => new OrderItemResponseDto
                    {
                        ProductId = i.ProductId,
                        ProductName = i.Product?.Name ?? string.Empty,
                        Quantity = i.Quantity,
                        UnitPrice = i.Product?.Price ?? 0
                    })
            ]
        };

        return Ok(response);
    }
}
