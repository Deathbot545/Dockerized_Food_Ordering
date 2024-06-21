
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Order_API.Data;
using Order_API.DTO;
using Order_API.Hubs;
using Order_API.Models;
using Order_API.Service.Orderser;
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
        private readonly OrderDbContext _context;

        public OrderApiController(IOrderService orderService, IHubContext<OrderStatusHub> hubContext, ILogger<OrderApiController> logger,OrderDbContext context)
         {
             _orderService = orderService;
             _hubContext = hubContext;
             _logger = logger;
            _context = context;
         }
        //ll
        [HttpGet("GetOrder/{id}")]
        public async Task<ActionResult<Order>> GetOrder(int id)
        {
            _logger.LogInformation("Fetching order with ID: {OrderId}", id);

            var order = await _context.Orders.FindAsync(id);

            if (order == null)
            {
                _logger.LogInformation("Order with ID {OrderId} not found", id);
                return NotFound();
            }

            _logger.LogInformation("Order with ID {OrderId} found. Loading order details...", id);

            await _context.Entry(order)
                .Collection(o => o.OrderDetails)
                .LoadAsync();

            foreach (var detail in order.OrderDetails)
            {
                _logger.LogInformation("Loading extra items for order detail with ID: {OrderDetailId}", detail.Id);

                await _context.Entry(detail)
                    .Collection(d => d.ExtraItems)
                    .LoadAsync();
            }

            _logger.LogInformation("Order with ID {OrderId} loaded successfully", id);

            return order;
        }


        // Add item to cart
        [HttpPost("AddOrder")]
        public async Task<IActionResult> AddOrder([FromBody] CartRequest request)
        {
            try
            {
                int orderId = await _orderService.ProcessOrderRequestAsync(request);

                var orderDto = await _orderService.GetOrderDetailsAsync(orderId);
                if (orderDto != null)
                {
                    _logger.LogInformation("Order details fetched successfully. Order details: {@OrderDto}", orderDto);
                    await _hubContext.Clients.All.SendAsync("NewOrderPlaced", orderDto);
                    return Ok(new { orderId = orderId, message = "Order processed successfully." });
                }
                else
                {
                    _logger.LogWarning("Order not found after creation.");
                    return NotFound(new { message = "Order not found after creation." });
                }
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
            if (updateOrderDto == null || updateOrderDto.OrderId <= 0)
            {
                return BadRequest("Invalid request data.");
            }

            await _orderService.UpdateOrderStatusAsync(updateOrderDto.OrderId, updateOrderDto.Status);

            // Notify all clients watching this order, including kitchen
            await _hubContext.Clients.Group("OrderGroup" + updateOrderDto.OrderId).SendAsync("ReceiveOrderUpdate", updateOrderDto);
            await _hubContext.Clients.Group("KitchenGroup").SendAsync("ReceiveOrderUpdate", updateOrderDto);

            return Ok();
        }


        [HttpGet("GetOrdersForOutlet/{outletId}")]
        public async Task<IActionResult> GetOrdersForOutlet(int outletId)
        {
            try
            {
                var ordersDTO = await _orderService.GetOrdersByOutletIdAsync(outletId);
                return Ok(ordersDTO);  // Ensure ordersDTO does not contain non-serializable types
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Internal server error occurred while fetching orders for outlet ID: {OutletId}", outletId);
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

         [HttpGet("GetOrdersByUser/{userId}")]
         public async Task<IActionResult> GetOrdersByUser(string userId)
         {
             try
             {
                 // Await the asynchronous call to get the orders
                 var orders = await _orderService.GetOrdersByUserIdAsync(userId);

                 // Since we're using async/await, orders should never be null (an awaitable task returns a result or throws)
                 // Therefore, it's safe to check for emptiness directly
                 if (!orders.Any())
                 {
                     return NotFound(new { message = "No orders found for the specified user." });
                 }

                 return Ok(orders);
             }
             catch (Exception ex)
             {
                 _logger.LogError($"Error retrieving orders for user {userId}: {ex.Message}");
                 return BadRequest(new { message = ex.Message });
             }
         }

        [HttpPost("CallWaiter")]
        public async Task<IActionResult> CallWaiter([FromBody] CallWaiterRequest request)
        {
            if (string.IsNullOrEmpty(request.TableId))
            {
                _logger.LogError("CallWaiter: Table ID is required.");
                return BadRequest("Table ID is required.");
            }

            _logger.LogInformation($"CallWaiter: Received request to call waiter for table ID {request.TableId}.");

            await _hubContext.Clients.Group("KitchenGroup").SendAsync("ReceiveWaiterCall", request.TableId);

            _logger.LogInformation($"CallWaiter: Sent waiter call for table ID {request.TableId} to KitchenGroup.");

            return Ok(new { message = "Waiter call sent successfully." });
        }

    }

    public class CallWaiterRequest
    {
        public string TableId { get; set; }
    }

}

