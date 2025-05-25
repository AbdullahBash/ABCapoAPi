using ABCapoAPi.Data;
using ABCapoAPi.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ABCapoAPi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController(AppDbContext _context) : ControllerBase
{
    [HttpPost("pay")]
    public async Task<IActionResult> Pay(PaymentDto dto)
    {
        var order = await _context.Orders.FindAsync(dto.OrderId);
        if (order is null)
            return NotFound("Order not found.");

        if (dto.Amount < order.TotalAmount)
            return BadRequest("Payment amount is less than order total.");

        var payment = new Payment
        {
            OrderId = dto.OrderId,
            Amount = dto.Amount,
            PaymentDate = DateTime.UtcNow
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Payment successful", PaymentId = payment.Id });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Payment>> GetPayment(int id)
    {
        var payment = await _context.Payments
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.Id == id);

        return payment is null ? NotFound() : payment;
    }
}
