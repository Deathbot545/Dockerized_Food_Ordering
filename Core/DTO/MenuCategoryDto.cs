using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO
{
    public class MenuCategoryDto
    {
        public int Id { get; set; }  // Unique identifier for each category
        public int OutletId { get; set; }
        public string CategoryName { get; set; }
        public string InternalOutletName { get; set; }
    }

}
