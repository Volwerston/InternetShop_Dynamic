using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InternetShop_Dynamic.Models
{
    public class UserLogItem
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Title { get; set; }
        public int Items { get; set; }
        public double Price { get; set; }
        public DateTime PurchaseTime { get; set; }
    }
}