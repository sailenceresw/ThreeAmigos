using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace ThreeAmigos.Areas.Identity.Data
{
     public class ThreeAmigosUser : IdentityUser
    { 
        public string? FullName { get; set; }
        public string? PaymentAddress { get; set; }
        public string? DeliveryAddress { get; set; }
        public string? TelephoneNumber { get; set; }

         
 
    }
}
