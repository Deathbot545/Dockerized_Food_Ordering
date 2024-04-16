
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
            _logger.LogInformation("Starting to process order request.");
            int? orderId = null;

            foreach (var item in request.MenuItems)
            {
                _logger.LogInformation($"Processing menu item with Id {item.Id}");

                // Construct the URL to the Menu API endpoint
                string url = $"https://restosolutionssaas.com/api/MenuApi/GetMenuItem/{item.Id}";

                try
                {
                    // Make the HTTP GET request to the Menu API
                    var response = await _httpClient.GetAsync(url);
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError($"MenuItem with Id {item.Id} not found. Status code: {response.StatusCode}");
                        throw new Exception($"MenuItem with Id {item.Id} not found.");
                    }

                    // Assuming your Menu API returns a JSON object that matches the MenuItemDto structure
                    var menuItemDto = await response.Content.ReadAsAsync<MenuItemDto>();

                    MenuItem menuItem = new MenuItem
                    {
                        Id = menuItemDto.id,
                        Name = menuItemDto.Name,
                        Description = menuItemDto.Description,
                        Price = menuItemDto.Price,
                        MenuCategoryId = menuItemDto.MenuCategoryId,
                        Image = Convert.FromBase64String(menuItemDto.Image)
                    };
                    orderId = await AddToCartAsync(menuItem, item.Qty, request.UserId, request.TableId, request.OutletId);
                    _logger.LogInformation($"Added item to cart. Temporary orderId: {orderId}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"An error occurred while fetching menu item {item.Id}: {ex.Message}");
                    throw;
                }
            }

            if (!orderId.HasValue)
            {
                _logger.LogError("No items were processed for the cart.");
                throw new Exception("No items were processed for the cart.");
            }

            _logger.LogInformation($"Order processed successfully with orderId: {orderId.Value}");
            return orderId.Value;
        }


        public async Task<int> AddToCartAsync(MenuItem menuItem, int quantity, string userId = null, int tableId = 0, int outletId = 0)
         {
             if (string.IsNullOrEmpty(userId))
             {
                 return await AddToCartAsGuestAsync(menuItem, quantity, tableId, outletId);
             }
             else
             {
                 return await AddToCartAsAuthenticatedUserAsync(menuItem, quantity, userId, tableId, outletId);
             }
         }


         private async Task<int> AddToCartAsGuestAsync(MenuItem menuItem, int quantity, int tableId, int outletId)
         {
             _logger.LogInformation("Creating a new order for guest.");

             // Create a new order every time this method is called to ensure uniqueness
             var newOrder = new Order
             {
                 OrderTime = DateTime.UtcNow,
                 Status = OrderStatus.Pending,
                 TableId = tableId,
                 OutletId = outletId
             };

             // Prepare a new order detail for this order
             var orderDetail = new OrderDetail
             {
                 MenuItemId = menuItem.Id,
                 Quantity = quantity
             };

             // Adding the order detail to the new order
             // Ensure that OrderDetails is initialized or check for null before adding to it
             newOrder.OrderDetails = new List<OrderDetail> { orderDetail };

             // Add the new order to the context and save changes
             _context.Orders.Add(newOrder);
             await _context.SaveChangesAsync();

             _logger.LogInformation($"New order created successfully with OrderId: {newOrder.Id}");

             return newOrder.Id; // Return the ID of the newly created order
         }



         private async Task<int> AddToCartAsAuthenticatedUserAsync(MenuItem menuItem, int quantity, string userId, int tableId, int outletId)
         {
             _logger.LogInformation("Creating new order for authenticated user.");



             var currentOrder = new Order
             {
                 OrderTime = DateTime.UtcNow,
                 Customer = userId,
                 Status = OrderStatus.Pending,
                 TableId = tableId,
                 OutletId = outletId
             };

             _context.Orders.Add(currentOrder);

             var orderDetail = new OrderDetail
             {
                 MenuItemId = menuItem.Id,
                 Quantity = quantity
             };

             currentOrder.OrderDetails = new List<OrderDetail> { orderDetail };

             await _context.SaveChangesAsync();
             _logger.LogInformation($"Order for authenticated user saved successfully. OrderId: {currentOrder.Id}");

             return currentOrder.Id;
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

            // Fetch orders from the local database
            var orders = _context.Orders
                .Include(o => o.OrderDetails)
                .Where(o => o.OutletId == outletId &&
                            o.Status != OrderStatus.Cancelled &&
                            o.Status != OrderStatus.Rejected &&
                            (o.Status != OrderStatus.Served || o.OrderTime > currentTime.AddHours(-1)))
                .ToList();

            // Fetch menu items from the Menu API
            var menuItemsDto = await FetchMenuItemsByOutletIdAsync(outletId);

            // Map to DTOs, converting MenuItemDto to MenuItemData
            return orders.Select(o => new OrderDTO
            {
                Id = o.Id,
                OrderTime = o.OrderTime,
                Customer = o.Customer,
                TableId = o.TableId,
                OutletId = o.OutletId,
                Status = o.Status,
                OrderDetails = o.OrderDetails.Select(od => new OrderDetailDTO
                {
                    Id = od.Id,
                    OrderId = od.OrderId,
                    MenuItemId = od.MenuItemId,
                    MenuItem = menuItemsDto.Where(mi => mi.id == od.MenuItemId)
                        .Select(mi => new MenuItemData
                        {
                            Id = mi.id,
                            Name = mi.Name,
                            Description = mi.Description,
                            Price = (double)mi.Price,
                            MenuCategoryId = mi.MenuCategoryId,
                            Image = mi.Image
                        }).FirstOrDefault(),
                    Quantity = od.Quantity
                }).ToList()
            }).ToList();
            _logger.LogInformation("Orders processed for serialization: {OrdersJson}", JsonSerializer.Serialize(ordersDTO, new JsonSerializerOptions { WriteIndented = true }));

            return ordersDTO;
        }


        private async Task<List<MenuItemDto>> FetchMenuItemsByOutletIdAsync(int outletId)
        {
            var menuItemsDto = new List<MenuItemDto>();

            // Construct the URL to the Menu API endpoint
            string url = $"https://restosolutionssaas.com/api/MenuApi/GetMenuItems/{outletId}";

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
                            Price = (double)mi.Price,
                            MenuCategoryId = mi.MenuCategoryId,
                            Image = mi.Image
                        }).FirstOrDefault(),
                    Quantity = od.Quantity
                }).ToList()
            };

            return orderDto;
        }

        public async Task<IEnumerable<OrderDTO>> GetOrdersByUserIdAsync(string userId)
        {
            var orders = await _context.Orders
                .Where(o => o.Customer == userId)
                .Include(o => o.OrderDetails)
                .ToListAsync();

            // Since we now know each order contains OutletId, we can directly fetch menu items for each order's outlet
            foreach (var order in orders)
            {
                var menuItems = await FetchMenuItemsByOutletIdAsync(order.OutletId);

                // Convert Order and its details to OrderDTO, incorporating fetched menu items
                order.OrderDetails.ForEach(od =>
                {
                    var menuItem = menuItems.FirstOrDefault(mi => mi.id == od.MenuItemId);
                    od.MenuItem = menuItem != null ? new MenuItemData
                    {
                        Id = menuItem.id,
                        Name = menuItem.Name,
                        Description = menuItem.Description,
                        Price = (double)menuItem.Price,
                        MenuCategoryId = menuItem.MenuCategoryId,
                        Image = menuItem.Image
                    } : null;
                });
            }

            // Convert the enriched orders to DTOs
            var orderDtos = orders.Select(o => new OrderDTO
            {
                Id = o.Id,
                OrderTime = o.OrderTime,
                Customer = o.Customer,
                TableId = o.TableId,
                OutletId = o.OutletId,
                Status = o.Status,
                OrderDetails = o.OrderDetails.Select(od => new OrderDetailDTO
                {
                    Id = od.Id,
                    OrderId = od.OrderId,
                    MenuItemId = od.MenuItemId,
                    MenuItem = od.MenuItem, // Now contains the MenuItemData converted from MenuItemDto
                    Quantity = od.Quantity
                }).ToList()
            }).ToList();

            return orderDtos;
        }

        // Assume FetchMenuItemsByOutletIdAsync and MenuItemData are defined similarly to your previous context.


    }

}
