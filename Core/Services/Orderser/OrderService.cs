using Core.DTO;
using Infrastructure.Data;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
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

        public OrderService(AppDbContext context)
        {
            _context = context;
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
            var orders = _context.Orders
                                 .Include(o => o.OrderDetails)
                                 .ThenInclude(od => od.MenuItem)
                                 .Where(o => o.OutletId == outletId)
                                 .ToList();

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
                        Price = (double)od.MenuItem.Price,  // Explicit cast here
                        MenuCategoryId = od.MenuItem.MenuCategoryId,
                        // If MenuCategory has more detailed properties you need, map them here
                        MenuCategory = od.MenuItem.MenuCategory  // If this doesn't work, you might need further mapping
                                                                 // Not mapping the image since you don't need it
                    },
                    Quantity = od.Quantity
                }).ToList()
            }).ToList();
        }




    }

}
