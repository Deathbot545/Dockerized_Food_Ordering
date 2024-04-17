using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Order_API.Models
{
    public class Order
    {
        public int Id { get; set; }
        public DateTime OrderTime { get; set; }
        public string? Customer { get; set; } // null if the user is a guest
        public int TableId { get; set; }
        public int OutletId { get; set; }
        public OrderStatus Status { get; set; } // Enum for Order Status: Pending, Preparing, Ready, Served
        public virtual List<OrderDetail> OrderDetails { get; set; } // individual items in the order
    }

}
