using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace Core.ViewModels
{
    public class KitchenStaffViewModel
    {
        public int? Id { get; set; }
        [Required]
        [Display(Name = "Name")]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        public int OutletId { get; set; } // Ensure this is included to link the staff to an outlet

        // Add a Role property
        [Required]
        [Display(Name = "Role")]
        public string Role { get; set; }
    }


}
