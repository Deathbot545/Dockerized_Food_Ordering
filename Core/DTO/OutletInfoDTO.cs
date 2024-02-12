using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO
{
    public class OutletInfoDTO
    {
        public string CustomerFacingName { get; set; }
        public byte[] Logo { get; set; }
        public byte[] RestaurantImage { get; set; }
        public TimeSpan OperatingHoursStart { get; set; }
        public TimeSpan OperatingHoursEnd { get; set; }
        public string Contact { get; set; }
    }
}
