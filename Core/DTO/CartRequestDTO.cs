using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO
{
    public class CartRequestDTO
    {
        public string UserId { get; set; } // Optional; null for guests
        public int TableId { get; set; }
        public int OutletId { get; set; }
        public List<SimpleCartItemDTO> Items { get; set; }
    }
}
