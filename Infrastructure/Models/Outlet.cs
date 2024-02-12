using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Models
{
    public class Outlet
    {
        public Outlet()
        {
            Tables = new List<Table>(); // Initialize to empty list here
        }
        [Key] public int Id { get; set; }
        public string InternalOutletName { get; set; }
        public string CustomerFacingName { get; set; }
        public string BusinessType { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string StreetAddress { get; set; }
        public string PostalCode { get; set; }  
        [DataType(DataType.Date)]
        public DateTime DateOpened { get; set; }
        public string Description { get; set; }
        public bool HealthAndSafetyCompliance { get; set; }
        public int EmployeeCount { get; set; }
        [DataType(DataType.Time)]
        public TimeSpan OperatingHoursStart { get; set; }
        [DataType(DataType.Time)]
        public TimeSpan OperatingHoursEnd { get; set; }
        public string Contact { get; set; }
        [Required(ErrorMessage = "You must agree to the terms and conditions.")]
        public bool AgreeToTerms { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public Guid OwnerId { get; set; } // Foreign key if you have a User model
        public byte[] Logo { get; set; }
        public byte[] RestaurantImage { get; set; }
        public ICollection<Table> Tables { get; set; } // one-to-many with Table
            public int MenuId { get; set; }  // Foreign key for Menu

        public string Subdomain { get; set; }
    }
}
