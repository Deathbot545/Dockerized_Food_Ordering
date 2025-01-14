﻿

using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
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
        Task UpdateOrderStatusAsync(string orderId, OrderStatus status);
        Task<string> ProcessOrderRequestAsync(CartRequest request);
        Task<IEnumerable<OrderDTO>> GetOrdersByOutletIdAsync(int outletId);
        Task<OrderDTO> GetOrderByOrderIdAsync(string orderId);
        Task<bool> DeleteOrderAsync(string orderId);
        Task<int?> GetOrderStatusAsync(string orderId);




        // Add other order-related methods as needed
    }
}
