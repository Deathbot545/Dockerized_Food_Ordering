
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
        public MenuItemData MenuItem { get; set; }
        public int Quantity { get; set; }
        public string Note { get; set; }
        public string Size { get; set; } // Include the size in the DTO
    }

    public class MenuItemData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public double Price { get; set; }
        public int MenuCategoryId { get; set; }
        public string Image { get; set; }
    }
}
