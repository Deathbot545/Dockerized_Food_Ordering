using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.SignalR;

namespace Order_API.Hubs
{
    [EnableCors("AllowMyOrigins")]
    public class OrderStatusHub : Hub
    {
        // Dictionary to maintain mapping between connection ID and order ID
        private static readonly Dictionary<string, int> _connectionOrderMap = new Dictionary<string, int>();

        public override async Task OnConnectedAsync()
        {
            string orderId = Context.GetHttpContext().Request.Query["orderId"].ToString();

            if (!string.IsNullOrEmpty(orderId) && int.TryParse(orderId, out int orderIdInt))
            {
                lock (_connectionOrderMap) // Ensure thread safety when modifying the dictionary
                {
                    _connectionOrderMap[Context.ConnectionId] = orderIdInt;
                }
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            lock (_connectionOrderMap) // Ensure thread safety when modifying the dictionary
            {
                _connectionOrderMap.Remove(Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        // Method to get the connection ID based on order ID (you can use this in your API)
        public static string GetConnectionIdForOrder(int orderId)
        {
            lock (_connectionOrderMap) // Ensure thread safety when accessing the dictionary
            {
                return _connectionOrderMap.FirstOrDefault(x => x.Value == orderId).Key;
            }
        }
    }
}
