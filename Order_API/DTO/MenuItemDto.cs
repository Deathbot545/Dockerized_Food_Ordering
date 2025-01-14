﻿
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Order_API.DTO
{


    public class MenuItemDto
    {
        public int id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int MenuCategoryId { get; set; }
        public string CategoryName { get; set; } // new field
        public string Image { get; set; } // This should be a base64 encoded string
        public List<string> Sizes { get; set; } // List of sizes
        public List<ExtraItemDto> ExtraItems { get; set; } // List of extra items
    }


}
