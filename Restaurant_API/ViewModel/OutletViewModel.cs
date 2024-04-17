
using Order_API.DTO;
using Restaurant_API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant_API.ViewModels
{
    public class OutletViewModel
    {
        public OutletViewModel()
        {
            Tables = new List<Table>();
            Orders = new List<OrderDTO>();

        }
        public Outlet OutletInfo { get; set; }
        public List<Table> Tables { get; set; }
        public List<OrderDTO> Orders { get; set; }  // NOTE: Use OrderDTO instead of Order
    }


}
