using InternetShop_Dynamic.Models;
using InternetShop_Dynamic.Models.Auxiliary;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
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
       

        [HttpPost]
        public ActionResult Purchase()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Please authorize for performing purchase operation");
            }

            List<BasketParams> toPass = new List<BasketParams>();

            if(Session["basket"] != null)
            {
                Dictionary<string, BasketParams> basket = (Dictionary<string, BasketParams>)Session["basket"];
                if(basket.Keys.Count > 0)
                {
                    
                    foreach(var kvp in basket)
                    {
                        toPass.Add(kvp.Value);
                    }
                }
            }

            if(toPass.Count == 0)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Session has expired");
            }

            if (SendPurchaseMail(toPass))
            {
                LogUserPurchase(toPass);
                Session["basket"] = null;
            }

            return new HttpStatusCodeResult(HttpStatusCode.OK, "OK");
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


        #region Helper functions

        [NonAction]
        private bool SendPurchaseMail(List<BasketParams> toProcess)
        {
            try
            {
                string body = BuildPurchaseLetter(toProcess);

                SendMessage(User.Identity.Name, "Internet Shop Purchase", body);

                return true;
            }
            catch
            {
                return false;
            }
        }

        [NonAction]
        private void LogUserPurchase(List<BasketParams> toProcess)
        {
            List<UserLogItem> toPass = new List<UserLogItem>();
            UserLogItemFactory factory = new UserLogItemFactory();

            foreach(var item in toProcess)
            {
                toPass.Add(factory.Create(item));
            }

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Request.Cookies["access_token"].Value);

                client.BaseAddress = new Uri("http://localhost:13384");

                HttpResponseMessage msg = client.PostAsJsonAsync("/api/Log/LogPurchaseItems", toPass).Result;
            }
        }

        [NonAction]
        private string BuildPurchaseLetter(List<BasketParams> items)
        {
            StringBuilder body = new StringBuilder();

            double price = 0;

            body.Append(string.Format("Dear customer, {0}", Environment.NewLine));
            body.Append(string.Format("Here is the list of your purchase items: {0}", Environment.NewLine));

            foreach(var item in items)
            {
                price += Math.Round(item.Item1.Price*(1 - item.Item1.Discount / 100)*item.Item2, 2);

                body.Append(string.Format(@"<div style='border: 1px solid black; width: 100%; margin:0'>
                                            <h3 style='text-align: center'>{0}</h3>
                                            <p style='text-align: center'>{1}</p>
                                            <p style='text-align: center'>Price per item: <b>{2} $</b></p>
                                            <p style='text-align: center'>Discount: {3}</p>
                                            <p style='text-align: center'>Items: <b>{4}</b></p>
                                            </div>",
                                            item.Item1.Title,
                                            item.Item1.Description,
                                            item.Item1.Price,
                                            item.Item1.Discount,
                                            item.Item2)
                            );
            }

            body.Append(string.Format("<h2 style='text-align: center'>Overall price: {0} $</h2>", price));
            body.Append(string.Format("{0} {0} Kind regards, {0} Internet Shop", Environment.NewLine));

            return body.ToString();
        }

        [NonAction]
        private static void SendMessage(string msgTo, string msgSubject, string msgBody)
        {
            string smtpHost = "smtp.gmail.com";
            int smtpPort = 587;
            string smtpUserName = "btsemail1@gmail.com";
            string smtpUserPass = "btsadmin";

            SmtpClient client = new SmtpClient(smtpHost, smtpPort);
            client.UseDefaultCredentials = false;
            client.Credentials = new NetworkCredential(smtpUserName, smtpUserPass);
            client.EnableSsl = true;

            string msgFrom = smtpUserName;


            MailMessage message = new MailMessage(msgFrom, msgTo, msgSubject, msgBody);

            message.IsBodyHtml = true;


            try
            {
                client.Send(message);
            }
            catch
            {
            }
        }

        #endregion
    }
}