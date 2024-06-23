

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

        public string Id { get; set; } // Update Id to string if it's stored as ObjectId
        public DateTime OrderTime { get; set; }
        public string Customer { get; set; }
        public int TableId { get; set; }
        public string TableName { get; set; }
        public string OutletId { get; set; } // Change OutletId to string
        public OrderStatus Status { get; set; }
        public List<OrderDetailDTO> OrderDetails { get; set; }
    }


    public class OrderDetailDTO
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string MenuItemId { get; set; } // Keep MenuItemId as string
        public MenuItemData MenuItem { get; set; }
        public int Quantity { get; set; }
        public string Note { get; set; }
        public string Size { get; set; } // Include the size in the DTO
        public List<ExtraItemDto> ExtraItems { get; set; } // List of extra items

        public OrderDetailDTO()
        {
            ExtraItems = new List<ExtraItemDto>();
        }
    }
    public class MenuItemData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int MenuCategoryId { get; set; }
        public string Image { get; set; }
    }

    public class ExtraItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
    }

}
