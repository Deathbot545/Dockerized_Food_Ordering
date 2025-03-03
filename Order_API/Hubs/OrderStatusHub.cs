﻿using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.SignalR;
using Order_API.DTO;

namespace Order_API.Hubs
{
    [EnableCors("AllowMyOrigins")]
    public class OrderStatusHub : Hub
    {
        private static readonly Dictionary<string, int> _connectionOrderMap = new Dictionary<string, int>();
        private readonly ILogger _logger;

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var orderId = httpContext.Request.Query["orderId"].ToString();
            var isKitchen = httpContext.Request.Query["isKitchen"];

            if (!string.IsNullOrEmpty(orderId) && int.TryParse(orderId, out int orderIdInt))
            {
                lock (_connectionOrderMap)
                {
                    _connectionOrderMap[Context.ConnectionId] = orderIdInt;
                }
                await Groups.AddToGroupAsync(Context.ConnectionId, "OrderGroup" + orderId);
            }

            if (!string.IsNullOrEmpty(isKitchen) && bool.Parse(isKitchen))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "KitchenGroup");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            lock (_connectionOrderMap)
            {
                _connectionOrderMap.Remove(Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        public static string GetConnectionIdForOrder(int orderId)
        {
            lock (_connectionOrderMap)
            {
                return _connectionOrderMap.FirstOrDefault(x => x.Value == orderId).Key;
            }
        }

        public async Task CallWaiter(string tableId)
        {
            await Clients.Group("KitchenGroup").SendAsync("ReceiveWaiterCall", tableId);
        }
        public async Task SendOrderUpdate(OrderDTO orderDto)
        {
            _logger.LogInformation("Sending order update with details: {@OrderDto}", orderDto);
            await Clients.Group("KitchenGroup").SendAsync("ReceiveOrderUpdate", orderDto);
        }

    }
}
