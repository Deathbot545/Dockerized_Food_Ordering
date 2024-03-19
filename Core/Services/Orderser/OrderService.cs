using Core.DTO;
using Core.Services.MenuS;
using Infrastructure.Data;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Services.Orderser
{
    public class OrderService : IOrderService
    {
        private readonly AppDbContext _context; // Assuming you have a DbContext named ApplicationDbContext
        private readonly IMenuService _menuService;
        private readonly ILogger<OrderService> _logger;

        public OrderService(AppDbContext context, IMenuService menuService,ILogger<OrderService> logger)
        {
            _context = context;
            _menuService = menuService;
            _logger = logger;
        }
        public async Task<int> ProcessOrderRequestAsync(CartRequest request)
        {
            _logger.LogInformation("Starting to process order request.");
            int? orderId = null;

            foreach (var item in request.MenuItems)
            {
                _logger.LogInformation($"Processing menu item with Id {item.Id}");
                var menuItemDto = await _menuService.GetMenuItemByIdAsync(item.Id);
                if (menuItemDto == null)
                {
                    throw new Exception($"MenuItem with Id {item.Id} not found.");
                }

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

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogError($"User not found: {userId}");
                throw new Exception("Authentication error: User not found.");
            }

            var currentOrder = new Order
            {
                OrderTime = DateTime.UtcNow,
                Customer = user,
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

        public IEnumerable<OrderDTO> GetOrdersByOutletId(int outletId)
        {
            var currentTime = DateTime.UtcNow; // Adjust this based on your timezone requirements if needed

            // Retrieve orders filtering out Cancelled and Rejected, and Served more than an hour ago
            var orders = _context.Orders
                                 .Include(o => o.OrderDetails)
                                     .ThenInclude(od => od.MenuItem)
                                 .Where(o => o.OutletId == outletId &&
                                             o.Status != OrderStatus.Cancelled &&
                                             o.Status != OrderStatus.Rejected &&
                                             (o.Status != OrderStatus.Served || o.OrderTime > currentTime.AddHours(-1)))
                                 .AsEnumerable() // Switch to in-memory processing for sorting
                                 .OrderBy(o => o.Status) // Assuming enum values are in the correct order for sorting
                                 .ToList();

            // Map to DTOs
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
                    MenuItem = new MenuItemData
                    {
                        Id = od.MenuItem.Id,
                        Name = od.MenuItem.Name,
                        Description = od.MenuItem.Description,
                        Price = (double)od.MenuItem.Price, // Ensure correct casting
                        MenuCategoryId = od.MenuItem.MenuCategoryId
                        // Map additional properties as needed
                    },
                    Quantity = od.Quantity
                }).ToList()
            }).ToList();
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
                    .ThenInclude(od => od.MenuItem)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return null;
            }

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
                    MenuItem = new MenuItemData
                    {
                        Id = od.MenuItem.Id,
                        Name = od.MenuItem.Name,
                        Description = od.MenuItem.Description,
                        Price = Convert.ToDouble(od.MenuItem.Price), // Explicitly cast decimal to double
                        MenuCategoryId = od.MenuItem.MenuCategoryId,
                        MenuCategory = od.MenuItem.MenuCategory, // Adjust according to your model
                        Image = Convert.ToBase64String(od.MenuItem.Image) // Convert byte[] to string
                    },
                    Quantity = od.Quantity
                }).ToList()
            };

            return orderDto;
        }



    }

}
