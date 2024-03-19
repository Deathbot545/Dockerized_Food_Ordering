using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Models
{
    public enum OrderStatus
    {
        Pending,    // Just placed the order
        Preparing,  // Kitchen is working on it
        Ready,      // Ready to be served
        Served,     // Delivered to the table
        Cancelled,  // Order cancelled by the customer or the staff
        Rejected    // Order rejected by the kitchen for some reason
    }
}
