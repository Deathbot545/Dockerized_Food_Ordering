
using Menu_API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
using OrderExtraItem = Order_API.Models.ExtraItem;
using MenuExtraItem = Menu_API.Models.ExtraItem;


namespace Order_API.Service.Orderser
{
    public class OrderService : IOrderService
    {
         private readonly OrderDbContext _context; // Assuming you have a DbContext named ApplicationDbContext
        private readonly HttpClient _httpClient;
        private readonly ILogger<OrderService> _logger;

         public OrderService(OrderDbContext context, ILogger<OrderService> logger, IHttpClientFactory httpClientFactory)
         {
             _context = context;
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
         }


        public async Task<int> ProcessOrderRequestAsync(CartRequest request)
        {
            _logger.LogInformation("Starting to process order request with details: {@Request}", request);

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
                _logger.LogInformation($"Processing menu item with Id {item.Id} and note: {item.Note}");

                string url = $"https://restosolutionssaas.com/api/MenuApi/GetMenuItem/{item.Id}";

                try
                {
                    var response = await _httpClient.GetAsync(url);
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError($"MenuItem with Id {item.Id} not found. Status code: {response.StatusCode}");
                        throw new Exception($"MenuItem with Id {item.Id} not found.");
                    }

                    var menuItemDto = await response.Content.ReadAsAsync<MenuItemDto>();
                    if (menuItemDto == null || menuItemDto.id == 0)
                    {
                        _logger.LogError($"Invalid data received for MenuItem with Id {item.Id}");
                        throw new Exception($"Invalid data received for MenuItem with Id {item.Id}");
                    }

                    var orderDetail = new OrderDetail
                    {
                        MenuItemId = menuItemDto.id,
                        Quantity = item.Qty,
                        Note = item.Note,
                        Size = item.Size,
                        ExtraItems = item.ExtraItems?.Select(extraItem => new OrderExtraItem
                        {
                            Name = extraItem.Name,
                            Price = extraItem.Price
                        }).ToList()
                    };

                    _logger.LogInformation($"Adding order detail: {@orderDetail}");
                    order.OrderDetails.Add(orderDetail);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"An error occurred while fetching menu item {item.Id}: {ex.Message}");
                    throw;
                }
            }

            _logger.LogInformation("Final order before saving to the database: {@Order}", order);

            foreach (var detail in order.OrderDetails)
            {
                _logger.LogInformation($"Order detail before saving: {detail.MenuItemId}, {detail.Quantity}, {detail.Note}, {detail.Size}");
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Order processed successfully with orderId: {order.Id}");
            return order.Id;
        }





        public async Task UpdateOrderStatusAsync(int orderId, OrderStatus status)
         {
             // Retrieve the existing order from the database
             var existingOrder = await _context.Orders.FindAsync(orderId);
             if (existingOrder == null)
             {
                 throw new Exception("Order not found.");
             }

             // Update the status
             existingOrder.Status = status;

             // Save changes to the database
             await _context.SaveChangesAsync();
         }

        public async Task<IEnumerable<OrderDTO>> GetOrdersByOutletIdAsync(int outletId)
        {
            var currentTime = DateTime.UtcNow;

            _logger.LogInformation("Fetching orders for outletId {OutletId} at {CurrentTime}", outletId, currentTime);

            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ExtraItems)
                .AsSplitQuery() // Use split query
                .Where(o => o.OutletId == outletId &&
                            o.Status != OrderStatus.Cancelled &&
                            o.Status != OrderStatus.Rejected &&
                            (o.Status != OrderStatus.Served || o.OrderTime > currentTime.AddHours(-1)))
                .ToListAsync();

            _logger.LogInformation("Fetched {Count} orders for outletId {OutletId}", orders.Count, outletId);

            foreach (var order in orders)
            {
                _logger.LogInformation("Order ID {OrderId} has {OrderDetailsCount} OrderDetails", order.Id, order.OrderDetails.Count);

                foreach (var od in order.OrderDetails)
                {
                    _logger.LogInformation("OrderDetail ID {OrderDetailId}, MenuItemId {MenuItemId}, Quantity {Quantity}, Note {Note}, Size {Size}, ExtraItemsCount {ExtraItemsCount}",
                        od.Id, od.MenuItemId, od.Quantity, od.Note, od.Size, od.ExtraItems?.Count ?? 0);
                }
            }

            var menuItemsDto = await FetchMenuItemsByOutletIdAsync(outletId);

            _logger.LogInformation("Fetched {Count} menu items for outletId {OutletId}", menuItemsDto.Count, outletId);

            var orderDtos = orders.Select(o => new OrderDTO
            {
                Id = o.Id,
                OrderTime = o.OrderTime,
                Customer = o.Customer,
                TableId = o.TableId,
                OutletId = o.OutletId,
                Status = o.Status,
                OrderDetails = o.OrderDetails.Select(od =>
                {
                    var menuItem = menuItemsDto.FirstOrDefault(mi => mi.id == od.MenuItemId);

                    var detailDto = new OrderDetailDTO
                    {
                        Id = od.Id,
                        OrderId = od.OrderId,
                        MenuItemId = od.MenuItemId,
                        MenuItem = menuItem != null ? new MenuItemData
                        {
                            Id = menuItem.id,
                            Name = menuItem.Name,
                            Description = menuItem.Description,
                            Price = menuItem.Price,
                            MenuCategoryId = menuItem.MenuCategoryId,
                            Image = menuItem.Image
                        } : null,
                        Quantity = od.Quantity,
                        Note = od.Note,
                        Size = od.Size, // Include the size in the DTO
                        ExtraItems = od.ExtraItems != null
                            ? od.ExtraItems.Select(ei => new ExtraItemDto { Id = ei.Id, Name = ei.Name, Price = ei.Price }).ToList()
                            : new List<ExtraItemDto>()
                    };

                    _logger.LogInformation("Mapped OrderDetailDTO: Id={Id}, OrderId={OrderId}, MenuItemId={MenuItemId}, Quantity={Quantity}, Note={Note}, Size={Size}, ExtraItemsCount={ExtraItemsCount}",
                        detailDto.Id, detailDto.OrderId, detailDto.MenuItemId, detailDto.Quantity, detailDto.Note, detailDto.Size, detailDto.ExtraItems.Count);

                    return detailDto;
                }).ToList()
            }).ToList();

            _logger.LogInformation("Mapped {Count} orders to OrderDTOs", orderDtos.Count);

            return orderDtos;
        }

        private async Task<List<MenuItemDto>> FetchMenuItemsByOutletIdAsync(int outletId)
        {
            var menuItemsDto = new List<MenuItemDto>();

            // Construct the URL to the Menu API endpoint
            string url = $"https://restosolutionssaas.com/api/MenuApi/GetMenuItemsWithExtras/{outletId}";

            try
            {
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    // Deserialize the HTTP response content into a List<MenuItemDto>
                    menuItemsDto = await response.Content.ReadFromJsonAsync<List<MenuItemDto>>();
                }
                else
                {
                    _logger.LogError($"Failed to fetch menu items for outlet {outletId}. Status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while fetching menu items for outlet {outletId}: {ex.Message}");
            }

            return menuItemsDto;
        }




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

        public async Task<OrderDTO> GetOrderDetailsAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                _logger.LogWarning($"Order with orderId {orderId} not found.");
                return null;
            }

            var menuItems = await FetchMenuItemsByOutletIdAsync(order.OutletId);

            var orderDto = new OrderDTO
            {
                Id = order.Id,
                OrderTime = order.OrderTime,
                Customer = order.Customer,
                TableId = order.TableId,
                OutletId = order.OutletId,
                Status = order.Status,
                OrderDetails = order.OrderDetails.Select(od => new OrderDetailDTO
                {
                    Id = od.Id,
                    OrderId = od.OrderId,
                    MenuItemId = od.MenuItemId,
                    MenuItem = menuItems.Where(mi => mi.id == od.MenuItemId)
                        .Select(mi => new MenuItemData
                        {
                            Id = mi.id,
                            Name = mi.Name,
                            Description = mi.Description,
                            Price = mi.Price,
                            MenuCategoryId = mi.MenuCategoryId,
                            Image = mi.Image
                        }).FirstOrDefault(),
                    Quantity = od.Quantity,
                    Note = od.Note,
                    Size = od.Size // Include the size in the DTO
                }).ToList()
            };

            _logger.LogInformation($"OrderDTO: {orderDto.Id}, {orderDto.OrderTime}, {orderDto.Customer}, {orderDto.TableId}, {orderDto.OutletId}, {orderDto.Status}");
            foreach (var detail in orderDto.OrderDetails)
            {
                _logger.LogInformation($"OrderDetailDTO: {detail.Id}, {detail.OrderId}, {detail.MenuItemId}, {detail.Quantity}, {detail.Note}, {detail.Size}");
            }

            return orderDto;
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
        }
    }
       
}
