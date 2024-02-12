using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO
{
    public class OrderDTO
    {
        public OrderDTO()
        {
            OrderDetails = new List<OrderDetailDTO>();
        }

        public int Id { get; set; }
        public DateTime OrderTime { get; set; }
        public ApplicationUser Customer { get; set; }
        public int TableId { get; set; }
        public int OutletId { get; set; }
        public OrderStatus Status { get; set; }
        public List<OrderDetailDTO> OrderDetails { get; set; }
    }


    public class OrderDetailDTO
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int MenuItemId { get; set; }
        public MenuItemData MenuItem { get; set; }  // This line is corrected
        public int Quantity { get; set; }
    }

    public class MenuItemData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public double Price { get; set; }
        public int MenuCategoryId { get; set; }
        public object MenuCategory { get; set; } // This is null in your JSON, so ensure it can handle this!
        public string Image { get; set; }
    }


}
