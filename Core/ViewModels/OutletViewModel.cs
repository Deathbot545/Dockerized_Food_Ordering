using Core.DTO;
using Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.ViewModels
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
