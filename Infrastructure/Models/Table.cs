using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Models
{
    public class Table
    {
        [Key]
        public int Id { get; set; }
        public string TableIdentifier { get; set; }  // Could be a name or a number

        public int OutletId { get; set; }  // Foreign key for Outlet
        public Outlet Outlet { get; set; } // Navigation property for Outlet

        public QRCode QRCode { get; set; } // one-to-one with QRCode
    }

}
