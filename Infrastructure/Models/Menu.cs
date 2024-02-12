using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Models
{
    public class Menu
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }  // e.g., "Summer 2023 Menu"

        [ForeignKey("Outlet")]
        public int? OutletId { get; set; }
        public Outlet Outlet { get; set; }  // This can be null

        public ICollection<MenuCategory> MenuCategories { get; set; } // one-to-many with MenuCategory
    }
}
