using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO
{
    // Your DTO
    public class LoginDto
    {
        public string UsernameOrEmail { get; set; }  // renamed from Email to UsernameOrEmail
        public string Password { get; set; }
    }

}
