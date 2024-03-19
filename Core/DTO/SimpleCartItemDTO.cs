using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO
{
    public class SimpleCartItemDTO
    {
        public int Id { get; set; } // Ensure this matches the type in your database.
        public string Name { get; set; }
        public double Price { get; set; }
        public int Qty { get; set; }
        public long Timestamp { get; set; }
    }

}
