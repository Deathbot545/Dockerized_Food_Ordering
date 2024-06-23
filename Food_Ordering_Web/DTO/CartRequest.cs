using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Food_Ordering_Web.DTO
{
    public class CartRequest
    {
        public string? UserId { get; set; }
        public int TableId { get; set; }
        public int OutletId { get; set; }
        public List<CartItem> MenuItems { get; set; }
        public bool AddUtensils { get; set; }
    }

    public class CartItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Qty { get; set; }
        public string Note { get; set; }
        public string Size { get; set; }
        public decimal Price { get; set; }
        public List<ExtraItemRequest>? ExtraItems { get; set; }
    }

    public class ExtraItemRequest
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
    }
}
