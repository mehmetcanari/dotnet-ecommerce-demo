using System.Security.Claims;
using ECommerce.API.DTO.Request.Order;
using ECommerce.API.Services.Order;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.User;

[ApiController]
[Route("api/user/orders")]
public class UserOrderController : ControllerBase
{
    private readonly IOrderService _orderService;

    public UserOrderController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [Authorize(Roles = "User")]
    [HttpPost("create")]
    public async Task<IActionResult> CreateOrder([FromBody] OrderCreateRequestDto orderCreateRequestRequest)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            return Forbid("User identity not found");
        }

        var userEmail = userIdClaim.Value;

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            await _orderService.AddOrderAsync(orderCreateRequestRequest, userEmail);
            return Created($"orders", new { message = "Order created successfully" });
        }
        catch (Exception exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [Authorize(Roles = "User")]
    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Forbid("User identity not found");
            }

            var userEmail = userIdClaim.Value;

            var order = await _orderService.GetOrdersAsync(userEmail);
            return Ok(new { message = "Order fetched successfully", data = order });
        }
        catch (Exception exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [Authorize(Roles = "User")]
    [HttpDelete("cancel-order")]
    public async Task<IActionResult> CancelOrder()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Forbid("User identity not found");
            }

            var userEmail = userIdClaim.Value;

            await _orderService.CancelOrderAsync(userEmail);
            return Ok(new { message = "Order deleted successfully" });
        }
        catch (Exception exception)
        {
            return BadRequest(exception.Message);
        }
    }
}