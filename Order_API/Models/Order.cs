using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Order_API.Models
{
    public class Order
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public DateTime OrderTime { get; set; }
        public string Customer { get; set; } // null if the user is a guest
        public int TableId { get; set; }
        public int OutletId { get; set; }
        public OrderStatus Status { get; set; } // Enum for Order Status: Pending, Preparing, Ready, Served
        public List<OrderDetail> OrderDetails { get; set; } // individual items in the order
    }

    public class OrderDetail
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string MenuItemId { get; set; }
        public string MenuItemName { get; set; } // New property to store the menu item name
        public int Quantity { get; set; }
        public string Note { get; set; }
        public string Size { get; set; }
        public List<ExtraItem> ExtraItems { get; set; } // Allow null
    }


    public class ExtraItem
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
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
