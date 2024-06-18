using Order_API.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Order_API.Models
{
    public class OrderDetail
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int MenuItemId { get; set; }
        public int Quantity { get; set; }
        public string Note { get; set; } // Note for special instructions
        public string Size { get; set; }
        public virtual List<ExtraItem>? ExtraItems { get; set; } // Allow null
    }

    public class ExtraItem
    {
        public int Id { get; set; }
        public int OrderDetailId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
    }


}
