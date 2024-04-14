using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Food_Ordering_Web.DTO
{
    public class QRCodeDTO
    {
        [Key]
        public int Id { get; set; }
        public byte[] Data { get; set; }
        public string MimeType { get; set; }

        [ForeignKey("Table")]  // Specify the foreign key here
        public int TableId { get; set; }
        public TableDTO Table { get; set; }  // Navigation property for Table
    }
}
