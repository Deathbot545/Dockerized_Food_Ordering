using Core.DTO;
using Core.Services.CartSter;
using Core.Services.MenuS;
using Core.Services.Orderser;
using Infrastructure.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Order_API.Hubs;

namespace Order_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("AllowMyOrigins")]
    public class OrderApiController : Controller
    {
       
        private readonly ICartService _cartService;
        private readonly IHubContext<OrderStatusHub> _hubContext;
        private readonly IOrderService _orderService;

        public OrderApiController(IOrderService orderService, ICartService cartService, IHubContext<OrderStatusHub> hubContext)
        {
            _orderService = orderService;
            _cartService = cartService;
            _hubContext = hubContext;
        }

        // Add item to cart
        [HttpPost("AddToCart")]
        public async Task<IActionResult> AddToCart([FromBody] CartRequest request)
        {
            try
            {
                int orderId = await _cartService.ProcessCartRequestAsync(request);
                await _hubContext.Clients.All.SendAsync("NewOrderPlaced", request);
                return Ok(new { orderId = orderId, message = "Items added to cart successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpPost]
        [Route("UpdateOrderStatus")]
        public async Task<IActionResult> UpdateOrderStatus(UpdateOrderStatusDto updateOrderDto)
        {
            // Validate the DTO
            if (updateOrderDto == null || updateOrderDto.OrderId <= 0)
            {
                return BadRequest("Invalid request data.");
            }

            // Use your service to update the order status in the database.
            await _orderService.UpdateOrderStatusAsync(updateOrderDto.OrderId, updateOrderDto.Status);

            // Fetch the connection ID for the order ID
            var connectionId = OrderStatusHub.GetConnectionIdForOrder(updateOrderDto.OrderId);

            // If a connection ID is found for the order ID, notify that specific client.
            if (!string.IsNullOrEmpty(connectionId))
            {
                await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveOrderUpdate", updateOrderDto);
            }

            return Ok();
        }

        [HttpGet("GetOrdersForOutlet/{outletId}")]
        public async Task<IActionResult> GetOrdersForOutlet(int outletId)
        {
            try
            {
                var ordersDTO = _orderService.GetOrdersByOutletId(outletId);
                return Ok(ordersDTO);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

    }
}
