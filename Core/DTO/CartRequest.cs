using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO
{
    public class CartRequest
    {
        public List<CartItem> MenuItems { get; set; }
        public int TableId { get; set; }
        public int OutletId { get; set; }
        public string? UserId { get; set; }

    }

    public class CartItem
    {
        public int Id { get; set; }  // Change type to int
        public int Qty { get; set; }
        public string Name { get; set; }  // Add this
        public decimal Price { get; set; }  // Assuming price is a decimal, add this
    }


}
