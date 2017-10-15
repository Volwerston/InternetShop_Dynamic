using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InternetShop_Dynamic.Models.Auxiliary
{
    public class PasswordRecoveryItem
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public DateTime ExpirationTime { get; set; }
    }
}