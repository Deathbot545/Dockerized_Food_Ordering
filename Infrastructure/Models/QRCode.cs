using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Models
{
    public class QRCode
    {
        [Key]
        public int Id { get; set; }
        public byte[] Data { get; set; }
        public string MimeType { get; set; }

        [ForeignKey("Table")]  // Specify the foreign key here
        public int TableId { get; set; }
        public Table Table { get; set; }  // Navigation property for Table
    }

}
