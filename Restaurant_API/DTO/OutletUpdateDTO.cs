using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant_API.DTO
{
    public class OutletUpdateDTO
    {
        public int Id { get; set; }
        public string? InternalOutletName { get; set; }
        public string? CustomerFacingName { get; set; }
        public string? BusinessType { get; set; }
        public string? Country { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Zip { get; set; }
        public string? StreetAddress { get; set; }
        public string? PostalCode { get; set; }
        public DateTime? DateOpened { get; set; }
        public string? Description { get; set; }
        public int? EmployeeCount { get; set; }
        public TimeSpan? OperatingHoursStart { get; set; }
        public TimeSpan? OperatingHoursEnd { get; set; }
        public string? Contact { get; set; }
  
    }


}
