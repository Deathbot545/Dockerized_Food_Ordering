

using Kitchen_Web.DTO;

namespace Kitchen_Web.ViewModel
{
    public class OutletViewModel
    {
        public OutletViewModel()
        {
            Tables = new List<TableDTO>();
            Orders = new List<OrderDTO>();

        }
        public List<TableDTO> Tables { get; set; }
        public List<OrderDTO> Orders { get; set; }  // NOTE: Use OrderDTO instead of Order
    }
}
