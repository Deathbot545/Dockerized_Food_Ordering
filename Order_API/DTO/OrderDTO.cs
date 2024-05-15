
using Microsoft.EntityFrameworkCore;
using Order_API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Order_API.DTO
{
    public class OrderDTO
    {
        public OrderDTO()
        {
            OrderDetails = new List<OrderDetailDTO>();
        }

        public int Id { get; set; }
        public DateTime OrderTime { get; set; }
        // Changed to string to match the Order model and represent the customer as a user ID
        public string Customer { get; set; }
        public int TableId { get; set; }
        public string TableName { get; set; }
        public int OutletId { get; set; }
        public OrderStatus Status { get; set; }
        public List<OrderDetailDTO> OrderDetails { get; set; }
    }

    public class OrderDetailDTO
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int MenuItemId { get; set; }
        // Assuming MenuItemData is a simplified DTO for MenuItem; adjusted to not include a direct entity reference
        public MenuItemData MenuItem { get; set; }
        public int Quantity { get; set; }
    }

    public class MenuItemData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public double Price { get; set; }
        public int MenuCategoryId { get; set; }
        // This should be a simplified category information if needed, otherwise consider removing or adjusting based on actual usage
       
        public string Image { get; set; } // Assuming this is a base64 encoded string for the image
    }

}
