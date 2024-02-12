using Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO
{
    public class TableDto
    {
        public int Id { get; set; } // Add this line
        public string TableIdentifier { get; set; }
        public string QRCodeData { get; set; } // Base64 Encoded SVG Data
    }

    public static class TableDtoAndConverter
    {
        public static TableDto TableToDto(Table table)
        {
            return new TableDto
            {
                Id = table.Id, // Add this line
                TableIdentifier = table.TableIdentifier,
                QRCodeData = table.QRCode != null ? Convert.ToBase64String(table.QRCode.Data) : null
            };
        }
    }

}
