using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InternetShop_Dynamic.Models.Auxiliary
{
    public class UserLogItemFactory : IDtoFactory<BasketParams, UserLogItem>
    {
        public UserLogItem Create(BasketParams source)
        {
            return new UserLogItem()
            {
                Id = source.Item1.Id,
                Items = source.Item2,
                Price = source.Item1.Price,
                PurchaseTime = DateTime.Now,
                Title = source.Item1.Title
            };
        }
    }
}