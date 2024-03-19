using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Models
{
    public class KitchenStaff
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        // Other kitchen staff specific properties

        // Foreign key to Outlet
        public int OutletId { get; set; }
        public Outlet Outlet { get; set; }

        // Optionally, include properties for authentication/authorization if needed
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; }
        // Consider a mechanism for roles or permissions if necessary
    }

}
