using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Menu_API.Models
{
    public class Menu
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }  // e.g., "Summer 2023 Menu"

        
        public int? OutletId { get; set; }

        public ICollection<MenuCategory> MenuCategories { get; set; } // one-to-many with MenuCategory
    }
}
