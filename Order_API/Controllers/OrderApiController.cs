
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
      //  private readonly OrderDbContext _context;

        public OrderApiController(IOrderService orderService, IHubContext<OrderStatusHub> hubContext, ILogger<OrderApiController> logger/*,OrderDbContext context*/)
         {
             _orderService = orderService;
             _hubContext = hubContext;
             _logger = logger;
           // _context = context;
         }

        [HttpPost("AddOrder")]
        public async Task<IActionResult> AddOrder([FromBody] CartRequest request)
        {
            _logger.LogInformation("Received order request: {@Request}", request);

            try
            {
                string orderId = await _orderService.ProcessOrderRequestAsync(request);

                _logger.LogInformation("Order processed successfully with orderId: {OrderId}", orderId);

                // Notify the kitchen about the new order
                await _hubContext.Clients.Group("KitchenGroup").SendAsync("NewOrderPlaced", orderId);

                return Ok(new { orderId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order");
                return BadRequest(new { message = ex.Message });
            }
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
        [HttpPost("UpdateOrderStatus")]
        public async Task<IActionResult> UpdateOrderStatus(UpdateOrderStatusDto updateOrderDto)
        {


            await _orderService.UpdateOrderStatusAsync(updateOrderDto.OrderId, updateOrderDto.Status);

            // Notify all clients watching this order, including kitchen
            await _hubContext.Clients.Group("OrderGroup" + updateOrderDto.OrderId).SendAsync("ReceiveOrderUpdate", updateOrderDto);
            await _hubContext.Clients.Group("KitchenGroup").SendAsync("ReceiveOrderUpdate", updateOrderDto);

            return Ok();
        }

        [HttpGet("GetOrderDetails/{orderId}")]
        public async Task<IActionResult> GetOrderDetails(string orderId)
        {
            try
            {
                var orderDetails = await _orderService.GetOrderByOrderIdAsync(orderId);
                if (orderDetails == null)
                {
                    _logger.LogError($"Order with Id {orderId} not found.");
                    return NotFound(new { message = $"Order with Id {orderId} not found." });
                }

                return Ok(orderDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving order details for orderId {orderId}: {ex.Message}");
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }
        [HttpDelete("DeleteOrder/{orderId}")]
        public async Task<IActionResult> DeleteOrder(string orderId)
        {
            try
            {
                var result = await _orderService.DeleteOrderAsync(orderId);
                if (result)
                {
                    return Ok(new { message = "Order deleted successfully." });
                }
                else
                {
                    return NotFound(new { message = $"Order with Id {orderId} not found." });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpGet("GetOrderStatus/{orderId}")]
        public async Task<IActionResult> GetOrderStatus(string orderId)
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



        /*

                

                  
                  

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
                 }*/

    }

    public class CallWaiterRequest
    {
        public string TableId { get; set; }
    }

}

