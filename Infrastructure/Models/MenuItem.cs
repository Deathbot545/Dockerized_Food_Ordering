using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Models
{
    public class MenuItem
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }  // e.g., "Chicken Rice", "Fish Rice"
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int MenuCategoryId { get; set; } // Foreign key for MenuCategory
        public MenuCategory MenuCategory { get; set; }  // Navigation property for MenuCategory
        public byte[] Image { get; set; } // new field to hold image data
    }
}
