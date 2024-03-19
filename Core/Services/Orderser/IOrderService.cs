using Core.DTO;
using Core.ViewModels;
using Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Services.Orderser
{
    public interface IOrderService
    {
      
        Task UpdateOrderStatusAsync(int orderId, OrderStatus status);
        IEnumerable<OrderDTO> GetOrdersByOutletId(int outletId);
        Task<int> ProcessOrderRequestAsync(CartRequest request);
        Task<int> AddToCartAsync(MenuItem menuItem, int quantity, string userId = null, int tableId = 0, int outletId = 0);
        Task<bool> DeleteOrderAsync(int orderId);
        Task<int?> GetOrderStatusAsync(int orderId);
        Task<OrderDTO> GetOrderDetailsAsync(int orderId);


        // Add other order-related methods as needed
    }
}
