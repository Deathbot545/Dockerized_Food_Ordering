using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Menu_API.DTO
{
    public class ExtraItemDto
    {
        public int Id { get; set; }  // Unique identifier for each extra item
        public string Name { get; set; }  // e.g., "Extra Cheese", "Extra Onions"
        public decimal Price { get; set; } // Price for this extra
    }

    public class MenuCategoryDto
    {
        public int Id { get; set; }  // Unique identifier for each category
        public int OutletId { get; set; }
        public string CategoryName { get; set; }
        public string InternalOutletName { get; set; }
        public List<ExtraItemDto> ExtraItems { get; set; } // List of extra items for this category
    }


}
