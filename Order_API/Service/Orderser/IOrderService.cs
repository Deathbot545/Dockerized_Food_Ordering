
using Menu_API.Models;
using Microsoft.AspNetCore.Mvc;
using Order_API.DTO;
using Order_API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Order_API.Service.Orderser
{
    public interface IOrderService
    {

        /* Task UpdateOrderStatusAsync(int orderId, OrderStatus status);
         Task<IEnumerable<OrderDTO>> GetOrdersByOutletIdAsync(int outletId);
         Task<int> ProcessOrderRequestAsync(CartRequest request);

         Task<bool> DeleteOrderAsync(int orderId);
         Task<int?> GetOrderStatusAsync(int orderId);
         Task<OrderDTO> GetOrderDetailsAsync(int orderId);
         Task<IEnumerable<OrderDTO>> GetOrdersByUserIdAsync(string userId);
 */

        Task<string> ProcessOrderRequestAsync(CartRequest request);

        // Add other order-related methods as needed
    }
}
