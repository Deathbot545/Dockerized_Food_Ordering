using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Food_Ordering_API.Models
{
    public class ApplicationUser : IdentityUser
    {
        public bool IsSubscribed { get; set; }
    }
}
