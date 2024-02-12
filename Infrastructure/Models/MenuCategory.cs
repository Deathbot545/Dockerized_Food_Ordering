using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Models
{
    public class MenuCategory
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }  // e.g., "Rice", "Sea Food", "Beverages"

        public int MenuId { get; set; }  // Foreign key for Menu
        public Menu Menu { get; set; }  // Navigation property for Menu

        public ICollection<MenuItem> MenuItems { get; set; } // one-to-many with MenuItem
    }
}
