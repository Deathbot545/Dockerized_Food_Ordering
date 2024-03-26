using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant_API.DTO
{
    public class AddTableDto // or AddTableViewModel if you prefer
    {
        public int OutletId { get; set; }
        public string TableIdentifier { get; set; }
    }

}
