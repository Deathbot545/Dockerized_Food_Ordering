using System.ComponentModel.DataAnnotations;

namespace Menu_API.Models
{
    public class MenuItemSize
    {
        [Key]
        public int Id { get; set; }
        public string Size { get; set; }  // e.g., "Small", "Medium", "Large"
        public decimal Price { get; set; } // Price for this size
        public int MenuItemId { get; set; } // Foreign key for MenuItem
        public MenuItem MenuItem { get; set; } // Navigation property for MenuItem
    }
}
