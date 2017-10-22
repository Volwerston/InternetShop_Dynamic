using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InternetShop_Dynamic.Models
{
    public class RestorePasswordEntry
    {
        public string Guid { get; set; }
        public string Email { get; set; }
        public string ConfirmPassword { get; set; }
        public string NewPassword { get; set; }
        public string UserId { get; set; }
        public DateTime AddingTime { get; set; }
    }
}