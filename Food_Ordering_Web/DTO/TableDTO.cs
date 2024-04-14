using System.ComponentModel.DataAnnotations;

namespace Food_Ordering_Web.DTO
{
    public class TableDTO
    {
        [Key]
        public int Id { get; set; }
        public string TableIdentifier { get; set; }  // Could be a name or a number

        public int OutletId { get; set; }  // Foreign key for Outlet
        public OutletDTO Outlet { get; set; } // Navigation property for Outlet

        public QRCodeDTO QRCode { get; set; } // one-to-one with QRCode
    }

}
