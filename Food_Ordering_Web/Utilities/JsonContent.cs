using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Core.Utilities
{
    public class JsonContent : StringContent
    {
        public JsonContent(object obj)
            : base(JsonSerializer.Serialize(obj), Encoding.UTF8, "application/json")
        {
        }
    }
}
