using Core.DTO;
using Core.Services.Orderser;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Order_API.Hubs;
using System.Text.Json;

namespace Order_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("AllowMyOrigins")]
    public class OrderApiController : Controller
    {
        private readonly IHubContext<OrderStatusHub> _hubContext;
        private readonly IOrderService _orderService;
        private ILogger<OrderApiController> _logger;    

        public OrderApiController(IOrderService orderService, IHubContext<OrderStatusHub> hubContext, ILogger<OrderApiController> logger)
        {
            _orderService = orderService;
            _hubContext = hubContext;
            _logger = logger;
        }
       
        // Add item to cart
        [HttpPost("AddOrder")]
        public async Task<IActionResult> AddOrder([FromBody] CartRequest request)
        {
            _logger.LogInformation($"Received order data: {JsonSerializer.Serialize(request)}");

            try
            {
                int orderId = await _orderService.ProcessOrderRequestAsync(request);
                await _hubContext.Clients.All.SendAsync("NewOrderPlaced", request);
                return Ok(new { orderId = orderId, message = "Order processed successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing order: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpPost("UpdateOrderStatus")]
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

        [HttpDelete("DeleteOrder/{orderId}")]
        public async Task<IActionResult> DeleteOrder(int orderId)
        {
            try
            {
                await _orderService.DeleteOrderAsync(orderId);
                return Ok(new { message = "Order deleted successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("GetOrderStatus/{orderId}")]
        public async Task<IActionResult> GetOrderStatus(int orderId)
        {
            try
            {
                var status = await _orderService.GetOrderStatusAsync(orderId);
                if (status == null)
                {
                    _logger.LogError($"Order with Id {orderId} not found or has no status.");
                    return NotFound(new { message = $"Order with Id {orderId} not found or has no status." });
                }

                _logger.LogInformation($"Order status for OrderId {orderId}: {status}");
                return Ok(new { orderId = orderId, status = status });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving order status for orderId {orderId}: {ex.Message}");
                return StatusCode(500, "Internal Server Error");
            }
        }
        [HttpGet("GetOrderDetails/{orderId}")]
        public async Task<IActionResult> GetOrderDetails(int orderId)
        {
            try
            {
                var orderDetails = await _orderService.GetOrderDetailsAsync(orderId);
                if (orderDetails == null)
                {
                    _logger.LogError($"Order with Id {orderId} not found.");
                    return NotFound(new { message = $"Order with Id {orderId} not found." });
                }

                // Assuming orderDetails is an object that includes order ID, status, and a list of items
                return Ok(orderDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving order details for orderId {orderId}: {ex.Message}");
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }




    }
}
