using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Menu_API.Models
{
    public class MenuCategory
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }  // e.g., "Rice", "Sea Food", "Beverages"
        public int MenuId { get; set; }  // Foreign key for Menu
        public Menu Menu { get; set; }  // Navigation property for Menu
        public ICollection<MenuItem> MenuItems { get; set; } // one-to-many with MenuItem
        public ICollection<ExtraItem> ExtraItems { get; set; } // one-to-many with ExtraItem
    }

    public class ExtraItem
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }  // e.g., "Extra Cheese", "Extra Onions"
        public decimal Price { get; set; } // Price for this extra
        public int MenuCategoryId { get; set; } // Foreign key for MenuCategory
        public MenuCategory MenuCategory { get; set; } // Navigation property for MenuCategory
    }

}
