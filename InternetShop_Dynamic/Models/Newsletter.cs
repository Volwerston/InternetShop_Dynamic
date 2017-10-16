using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InternetShop_Dynamic.Models
{
    public class Newsletter
    {
        public string Heading { get; set; }
        public string Text { get; set; }
        public bool AllUsers { get; set; }
        public string SpecificEmail { get; set; }
    }
}