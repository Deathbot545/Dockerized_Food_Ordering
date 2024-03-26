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
        //public virtual MenuItem MenuItem { get; set; }
        public int Quantity { get; set; }
        public MenuItemData MenuItem { get; internal set; }
    }
}
