using InternetShop_Dynamic.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace InternetShop_Dynamic.Controllers
{
    public class BasketController : Controller
    {
        [HttpPost]
        public ActionResult AddProductToBasket(BasketParams data)
        {
            try
            {
                if (Session["basket"] != null)
                {
                    Dictionary<string, BasketParams> basket = (Dictionary<string, BasketParams>)Session["basket"];

                    if (basket.ContainsKey(data.Item1.Title))
                    {
                        var toAdd = new BasketParams()
                        {
                            Item1 = data.Item1,
                            Item2 = data.Item2 + basket[data.Item1.Title].Item2
                        };

                        basket[data.Item1.Title] = toAdd;
                    }
                    else
                    {
                        basket.Add(data.Item1.Title, data);
                    }

                    Session["basket"] = basket;
                }
                else
                {
                    Dictionary<string, BasketParams> basket = new Dictionary<string, BasketParams>();

                    basket.Add(data.Item1.Title, data);

                    Session["basket"] = basket;
                }

                return new HttpStatusCodeResult(HttpStatusCode.OK, "OK");
            }
            catch (Exception e)
            {
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, e.Message);
            }
        }

        [HttpPost]
        public ActionResult Change(List<BasketParams> data)
        {
            try
            {
                if (data != null)
                {
                    Dictionary<string, BasketParams> basket = new Dictionary<string, BasketParams>();

                    foreach (var item in data)
                    {
                        basket.Add(item.Item1.Title, item);
                    }

                    Session["basket"] = basket;
                }
                else
                {
                    Session["basket"] = null;
                }

                return new HttpStatusCodeResult(HttpStatusCode.OK, "OK");
            }
            catch(Exception e)
            {
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, e.Message);
            }
        }

        [HttpGet]
        public string GetGoods()
        {
            List<BasketParams> toReturn = new List<BasketParams>();

            if(Session["basket"] != null)
            {
                toReturn = ((Dictionary<string, BasketParams>)Session["basket"]).Values.ToList();
            }

            return JsonConvert.SerializeObject(toReturn);
        }

        public ActionResult Basket()
        {
            return View();
        }

        #region Helper classes

        public class BasketParams
        {
            public Good Item1 { get; set; }
            public int Item2 { get; set; }
        }

        #endregion
    }
}