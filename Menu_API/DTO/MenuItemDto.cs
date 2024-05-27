using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Menu_API.DTO
{
    public class MenuItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public bool IsVegetarian { get; set; }
        public int MenuCategoryId { get; set; }
        public string CategoryName { get; set; }
        public string Image { get; set; }
        public List<MenuItemSizeDto> Sizes { get; set; } // List of sizes
    }

    public class MenuItemSizeDto
    {
        public string Size { get; set; }
        public decimal Price { get; set; }
    }

}
