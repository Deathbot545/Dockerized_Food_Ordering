

namespace Kitchen_Web.DTO
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
        public string MenuItemName { get; set; }
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
    public enum OrderStatus
    {
        Pending,    // Just placed the order
        Preparing,  // Kitchen is working on it
        Ready,      // Ready to be served
        Served,     // Delivered to the table
        Cancelled,  // Order cancelled by the customer or the staff
        Rejected    // Order rejected by the kitchen for some reason
    }
}
