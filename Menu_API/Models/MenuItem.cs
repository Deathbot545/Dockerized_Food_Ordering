using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Menu_API.Models
{
    public class MenuItem
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }  // e.g., "Chicken Rice", "Fish Rice"
        public string Description { get; set; }
        public decimal Price { get; set; }  // This can be a base price or default price
        public bool IsVegetarian { get; set; } // new field for vegetarian option
        public int MenuCategoryId { get; set; } // Foreign key for MenuCategory
        public MenuCategory MenuCategory { get; set; }  // Navigation property for MenuCategory
        public byte[] Image { get; set; } // field to hold image data
        public ICollection<MenuItemSize> MenuItemSizes { get; set; } // one-to-many relationship with MenuItemSize
    }

}
