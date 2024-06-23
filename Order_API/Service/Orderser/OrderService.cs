

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Order_API.Data;
using Order_API.DTO;
using Order_API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
// Add using directives for namespaces with aliases
using ExtraItem = Order_API.Models.ExtraItem;

namespace Order_API.Service.Orderser
{
    public class OrderService : IOrderService
    {
        // private readonly OrderDbContext _context; // Assuming you have a DbContext named ApplicationDbContext
        private readonly HttpClient _httpClient;
        private readonly ILogger<OrderService> _logger;
        private readonly MongoDBContext _context;

        public OrderService(MongoDBContext context, ILogger<OrderService> logger, IHttpClientFactory httpClientFactory)
         {
             _context = context;
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
         }


       
        public async Task<string> ProcessOrderRequestAsync(CartRequest request)
        {
            _logger.LogInformation("Starting to process order request with details: {RequestJson}", JsonConvert.SerializeObject(request));

            var order = new Order
            {
                OrderTime = DateTime.UtcNow,
                Customer = request.UserId,
                TableId = request.TableId,
                OutletId = request.OutletId,
                Status = OrderStatus.Pending,
                OrderDetails = new List<OrderDetail>()
            };

            foreach (var item in request.MenuItems)
            {
                _logger.LogInformation("Processing item: {ItemJson}", JsonConvert.SerializeObject(item));

                var extraItems = item.ExtraItems?.Select(extraItem => new ExtraItem
                {
                    Name = extraItem.Name,
                    Price = extraItem.Price
                }).ToList();

                var orderDetail = new OrderDetail
                {
                    MenuItemId = item.Id.ToString(),
                    Quantity = item.Qty,
                    Note = item.Note,
                    Size = item.Size,
                    ExtraItems = extraItems,
                    MenuItemName = item.Name // Set the MenuItemName from the CartItem
                };

                _logger.LogInformation("Adding order detail: {OrderDetailJson}", JsonConvert.SerializeObject(orderDetail));
                order.OrderDetails.Add(orderDetail);
            }


            _logger.LogInformation("Final order before saving to the database: {OrderJson}", JsonConvert.SerializeObject(order));

            try
            {
                await _context.Orders.InsertOneAsync(order);
                _logger.LogInformation("Order processed successfully with orderId: {OrderId}", order.Id);
                return order.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order");
                throw;
            }
        }


        public async Task UpdateOrderStatusAsync(string orderId, OrderStatus status)
        {
            _logger.LogInformation("Updating status for Order ID: {OrderId} to Status: {Status}", orderId, status);

            var filter = Builders<Order>.Filter.Eq(o => o.Id, orderId);
            var update = Builders<Order>.Update.Set(o => o.Status, status);

            var result = await _context.Orders.UpdateOneAsync(filter, update);

            if (result.ModifiedCount == 0)
            {
                _logger.LogWarning("No order found with ID: {OrderId}", orderId);
                throw new Exception("Order not found.");
            }

            _logger.LogInformation("Order ID: {OrderId} successfully updated to Status: {Status}", orderId, status);
        }

        public async Task<IEnumerable<OrderDTO>> GetOrdersByOutletIdAsync(int outletId)
        {
            var currentTime = DateTime.UtcNow;

            // Fetch orders from MongoDB
            var orders = await _context.Orders
                .Find(o => o.OutletId == outletId &&
                           o.Status != OrderStatus.Cancelled &&
                           o.Status != OrderStatus.Rejected &&
                           (o.Status != OrderStatus.Served || o.OrderTime > currentTime.AddHours(-1)))
                .ToListAsync();

            // Transform orders to DTOs
            var orderDtos = orders.Select(o => new OrderDTO
            {
                Id = o.Id,
                OrderTime = o.OrderTime,
                Customer = o.Customer,
                TableId = o.TableId,
                OutletId = o.OutletId.ToString(), // Convert OutletId to string
                Status = o.Status,
                OrderDetails = o.OrderDetails.Select(od => new OrderDetailDTO
                {
                    Id = int.TryParse(od.Id, out int id) ? id : 0, // Convert Id to int
                    OrderId = int.TryParse(o.Id, out int orderId) ? orderId : 0, // Convert OrderId to int
                    MenuItemId = od.MenuItemId, // Keep MenuItemId as string
                    MenuItemName=od.MenuItemName,
                    Quantity = od.Quantity,
                    Note = od.Note,
                    Size = od.Size,
                    ExtraItems = od.ExtraItems?.Select(ei => new ExtraItemDto
                    {
                        Id = int.TryParse(ei.Id, out int extraItemId) ? extraItemId : 0, // Convert ExtraItem Id to int
                        Name = ei.Name,
                        Price = ei.Price
                    }).ToList() ?? new List<ExtraItemDto>()
                }).ToList()
            }).ToList();

            return orderDtos;
        }


        public async Task<bool> DeleteOrderAsync(string orderId)
        {
            var orderObjectId = ObjectId.Parse(orderId);

            var order = await _context.Orders.Find(o => o.Id == orderObjectId.ToString()).FirstOrDefaultAsync();
            if (order == null)
            {
                // Order not found
                return false;
            }

            // Remove the order
            var deleteResult = await _context.Orders.DeleteOneAsync(o => o.Id == orderObjectId.ToString());

            return deleteResult.DeletedCount > 0; // Return true to indicate successful deletion
        }


        /*   public async Task<int?> GetOrderStatusAsync(string orderId)
           {
               var orderObjectId = ObjectId.Parse(orderId);

               var order = await _context.Orders.Find(o => o.Id == orderObjectId).FirstOrDefaultAsync();
               if (order == null)
               {
                   _logger.LogError($"Order with Id {orderId} not found.");
                   return null;
               }
               return (int)order.Status; // Return the integer value of the enum
           }*/

        public async Task<OrderDTO> GetOrderByOrderIdAsync(string orderId)
        {
            // Fetch order from MongoDB
            var order = await _context.Orders.Find(o => o.Id == orderId).FirstOrDefaultAsync();

            if (order == null)
            {
                // Order not found
                return null;
            }

            // Transform order to DTO
            var orderDto = new OrderDTO
            {
                Id = order.Id,
                OrderTime = order.OrderTime,
                Customer = order.Customer,
                TableId = order.TableId,
                OutletId = order.OutletId.ToString(), // Convert OutletId to string
                Status = order.Status,
                OrderDetails = order.OrderDetails.Select(od => new OrderDetailDTO
                {
                    Id = int.TryParse(od.Id, out int id) ? id : 0, // Convert Id to int
                    OrderId = int.TryParse(order.Id, out int orderId) ? orderId : 0, // Convert OrderId to int
                    MenuItemId = od.MenuItemId, // Keep MenuItemId as string
                    MenuItemName = od.MenuItemName,
                    Quantity = od.Quantity,
                    Note = od.Note,
                    Size = od.Size,
                    ExtraItems = od.ExtraItems?.Select(ei => new ExtraItemDto
                    {
                        Id = int.TryParse(ei.Id, out int extraItemId) ? extraItemId : 0, // Convert ExtraItem Id to int
                        Name = ei.Name,
                        Price = ei.Price
                    }).ToList() ?? new List<ExtraItemDto>()
                }).ToList()
            };

            return orderDto;
        }



        /*
          
         public async Task<bool> DeleteOrderAsync(int orderId)
          {
              var order = await _context.Orders.FindAsync(orderId);
              if (order == null)
              {
                  // Order not found
                  return false;
              }
              // e.g., order details, remove them here
              var orderDetails = _context.OrderDetails.Where(od => od.OrderId == orderId);
              _context.OrderDetails.RemoveRange(orderDetails);

              // Remove the order
              _context.Orders.Remove(order);

              // Save changes to the database
              await _context.SaveChangesAsync();

              return true; // Return true to indicate successful deletion
          }

          public async Task<int?> GetOrderStatusAsync(int orderId)
          {
              var order = await _context.Orders.FindAsync(orderId);
              if (order == null)
              {
                  _logger.LogError($"Order with Id {orderId} not found.");
                  return null;
              }
              return (int)order.Status; // Return the integer value of the enum
          }

        



         public async Task<IEnumerable<OrderDTO>> GetOrdersByUserIdAsync(string userId)
         {
             var orders = await _context.Orders
                 .Where(o => o.Customer == userId)
                 .Include(o => o.OrderDetails)
                 .ToListAsync();

             // Prepare DTOs to fill with data
             var orderDtos = new List<OrderDTO>();

             foreach (var order in orders)
             {
                 var menuItems = await FetchMenuItemsByOutletIdAsync(order.OutletId);

                 var orderDetailsDto = order.OrderDetails.Select(od =>
                 {
                     var menuItemData = menuItems.FirstOrDefault(mi => mi.id == od.MenuItemId);
                     return new OrderDetailDTO
                     {
                         Id = od.Id,
                         OrderId = od.OrderId,
                         MenuItemId = od.MenuItemId,
                         Quantity = od.Quantity,
                         MenuItem = menuItemData != null ? new MenuItemData
                         {
                             Id = menuItemData.id,
                             Name = menuItemData.Name,
                             Description = menuItemData.Description,
                             Price = menuItemData.Price,
                             MenuCategoryId = menuItemData.MenuCategoryId,
                             Image = menuItemData.Image
                         } : null,
                         Note = od.Note
                     };
                 }).ToList();

                 var orderDto = new OrderDTO
                 {
                     Id = order.Id,
                     OrderTime = order.OrderTime,
                     Customer = order.Customer,
                     TableId = order.TableId,
                     OutletId = order.OutletId,
                     Status = order.Status,
                     OrderDetails = orderDetailsDto
                 };

                 _logger.LogInformation($"OrderDTO: {orderDto.Id}, {orderDto.OrderTime}, {orderDto.Customer}, {orderDto.TableId}, {orderDto.OutletId}, {orderDto.Status}");
                 foreach (var detail in orderDto.OrderDetails)
                 {
                     _logger.LogInformation($"OrderDetailDTO: {detail.Id}, {detail.OrderId}, {detail.MenuItemId}, {detail.Quantity}, {detail.Note}");
                 }

                 orderDtos.Add(orderDto);
             }

             return orderDtos;
         }*/
    }

}
