using InternetShop_Dynamic.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace InternetShop_Dynamic.Controllers
{
    public class MainController : Controller
    {
        // GET: Main
        public ActionResult Index()
        {
            return View();
        }

        [Authorize]
        public async Task<ActionResult> AdminAccount()
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                client.BaseAddress = new Uri("http://localhost:13384");

                HttpResponseMessage msg = await client.GetAsync("/api/Goods/All");

                if (msg.IsSuccessStatusCode)
                {
                    var data = await msg.Content.ReadAsAsync<Tuple<List<Good>, List<Category>>>();

                    ViewData["categories"] = data.Item2;
                    ViewData["goods"] = data.Item1;
                }
            }

            return View();
        }

        [Authorize]
        public async Task<ActionResult> Product(int id)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Request.Cookies["access_token"].Value);

                    client.BaseAddress = new Uri("http://localhost:13384");

                    HttpResponseMessage msg = await client.GetAsync("/api/Goods/Good/" + id);

                    if (msg.IsSuccessStatusCode)
                    {
                        Good g = await msg.Content.ReadAsAsync<Good>();
                        return View(g);
                    }
                    else
                    {
                        throw new Exception("Product with this id not found");
                    }
                }
            }
            catch
            {
                return RedirectToAction("Index", "Main");
            }
        }

        public async Task<ActionResult> AddPhoto(int id, HttpPostedFileBase photo)
        {
            try
            {
                byte[] toPass = new byte[(int)photo.InputStream.Length];
                photo.InputStream.Read(toPass, 0, (int)photo.InputStream.Length);

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                    client.BaseAddress = new Uri("http://localhost:13384");

                    HttpResponseMessage msg = await client.PutAsJsonAsync("/api/Goods/AddPhoto/" + id, toPass);
                }

                return RedirectToAction("Product", "Main", new { id = id });
            }
            catch
            {
                return RedirectToAction("Product", "Main", new { id = id });
            }
        }

        [Authorize]
        public ActionResult SimpleAccount()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        public ActionResult SendNewsletter(Newsletter nl)
        {
            StringBuilder template = new StringBuilder();

            using (FileStream fs = new FileStream(Server.MapPath("~/Common/Templates/newsletter.html"), FileMode.Open, FileAccess.Read))
            {
                using (StreamReader rdr = new StreamReader(fs))
                {
                    template.Append(rdr.ReadToEnd());
                }
            }


            if (nl.AllUsers)
            {
                Task.Run(() => {
                    SendGlobalNewsletter(nl, template);
                });
            }
            else
            {
                Task.Run(() => {
                    SendNewsletterToUser(nl, new UserDto() { Email = nl.SpecificEmail }, template);
                });
            }

            return new HttpStatusCodeResult(HttpStatusCode.OK, "OK");
        }

        public async Task<ActionResult> Products()
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                client.BaseAddress = new Uri("http://localhost:13384");

                HttpResponseMessage msg = await client.GetAsync("/api/Goods/Categories");

                List<Category> categories = new List<Category>();

                if (msg.IsSuccessStatusCode)
                {
                    categories = await msg.Content.ReadAsAsync<List<Category>>();
                }

                ViewData["categories"] = categories;
            }
                return View();
        }

        #region Helper functions


        [NonAction]
        private void SendGlobalNewsletter(Newsletter nl, StringBuilder template)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Request.Cookies["access_token"].Value);

                    client.BaseAddress = new Uri("http://localhost:13384");

                    HttpResponseMessage msg = client.GetAsync("/api/Account/FindUsers?id=all").Result;

                    if (msg.IsSuccessStatusCode)
                    {
                        List<UserDto> users = msg.Content.ReadAsAsync<List<UserDto>>().Result;

                        foreach (var user in users)
                        {
                            Task.Run(() => { SendNewsletterToUser(nl, user, template); });
                        }
                    }
                }
            }
            catch
            {
            }
        }

        [NonAction]
        private void SendNewsletterToUser(Newsletter nl, UserDto user, StringBuilder template)
        {
            StringBuilder localTemplate = new StringBuilder(template.ToString());

            localTemplate = localTemplate.Replace("{email}", user.Email);
            localTemplate = localTemplate.Replace("{heading}", nl.Heading.ToUpper());
            localTemplate = localTemplate.Replace("{text}", nl.Text);

            SendMessage(user.Email, "Internet Shop Newsletter", localTemplate.ToString());
        }


        [NonAction]
        private static void SendMessage(string msgTo,string msgSubject,string msgBody)
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