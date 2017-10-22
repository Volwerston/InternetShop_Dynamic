using InternetShop_Dynamic.Models;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace InternetShop_Dynamic.Controllers
{
    public class AuthController : Controller
    {
        public ActionResult SignIn()
        {
            UserLoginViewModel model = new UserLoginViewModel();

            return View(model);
        }

        public ActionResult Register()
        {
            RegisterBindingModel model = new RegisterBindingModel();
            return View(model);
        }

        [HttpPost]
        public async Task<ActionResult> Register(RegisterBindingModel model)
        {
            if (ModelState.IsValid)
            {

                using (HttpClient client = new HttpClient())
                {
                    client.BaseAddress = new Uri("http://localhost:13384");

                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                    HttpResponseMessage msg = await client.PostAsJsonAsync("/api/Account/Register", model);


                    if (msg.IsSuccessStatusCode)
                    {
                        return RedirectToAction("Products", "Main");
                    }
                    else
                    {
                        List<string> res = msg.Content.ReadAsAsync<List<string>>().Result;
                        ViewData["errors"] = res;
                    }
                }
            }

            return View(model);
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult> RestorePassword(ChangePasswordBindingModel model)
        {
            if (ModelState.IsValid)
            {
                using (HttpClient client = new HttpClient())
                {
                    client.BaseAddress = new Uri("http://localhost:13384");

                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Request.Cookies["access_token"].Value);
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                    HttpResponseMessage msg = await client.PostAsJsonAsync("api/Account/ChangePassword", model);

                    if (msg.IsSuccessStatusCode)
                    {
                        return RedirectToAction("SignIn", "Auth");
                    }
                    else
                    {
                        List<string> res = msg.Content.ReadAsAsync<List<string>>().Result;
                        ViewData["errors"] = res;
                    }
                }
            }

            return View(model);
        }

        public ActionResult ChangePassword()
        {
            return View();
        }

        public async Task<ActionResult> RestorePassword(string guid)
        {
            RestorePasswordEntry entry = new RestorePasswordEntry();

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                client.BaseAddress = new Uri("http://localhost:13384");

                HttpResponseMessage msg = await client.GetAsync("/api/Account/GetPasswordRestoreEntry?guid=" + guid);

                if (msg.IsSuccessStatusCode)
                {
                    entry = msg.Content.ReadAsAsync<RestorePasswordEntry>().Result;

                    if(entry.AddingTime == default(DateTime))
                    {
                        ViewData["message"] = "Guid has already been used. Please send another restoration letter";
                    }
                    else if((DateTime.Now - entry.AddingTime).TotalHours > 24)
                    {
                        ViewData["message"] = "Guid has expired. Please send another restoration letter";
                    }
                }
                else
                {
                    ViewData["message"] = msg.Content.ReadAsAsync<string>().Result;
                }
            }

            return View(entry);
        }

        [HttpPost]
        public async Task<ActionResult> ChangePassword(string email)
        {
            bool emailExists = false;

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                client.BaseAddress = new Uri("http://localhost:13384");

                HttpResponseMessage msg = await client.GetAsync("/api/Account/EmailExists?email=" + email);

                if (msg.IsSuccessStatusCode)
                {
                    emailExists = msg.Content.ReadAsAsync<bool>().Result;
                }
                else
                {
                    string eMsg = msg.Content.ReadAsAsync<string>().Result;

                    return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, eMsg);
                }
            }

            if (emailExists)
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                    client.BaseAddress = new Uri("http://localhost:13384");

                    HttpResponseMessage msg = await client.GetAsync("/api/Account/AddPasswordRestorationEntry?email="+email);

                    if (msg.IsSuccessStatusCode)
                    {
                        string guid = msg.Content.ReadAsAsync<string>().Result;
                        SendRestorationEmail(email, guid);
                    }
                    else
                    {
                        string eMsg = msg.Content.ReadAsAsync<string>().Result;
                        return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, eMsg);
                    }
                }
            }
            else
            {
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "User with this email is not found");
            }

            return new HttpStatusCodeResult(HttpStatusCode.OK, "OK");
        }

        public async Task<ActionResult> AccountPage()
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Request.Cookies["access_token"].Value);
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                client.BaseAddress = new Uri("http://localhost:13384");

                HttpResponseMessage msg = await client.GetAsync("/api/Account/UserRoles");

                if (msg.IsSuccessStatusCode)
                {
                    List<string> roles = await msg.Content.ReadAsAsync<List<string>>();

                    if (roles.Contains("admin"))
                    {
                        return RedirectToAction("AdminAccount", "Main");
                    }
                }
            }

            return RedirectToAction("SimpleAccount", "Main");
        }



        #region Helper functions

        [NonAction]
        private void SendRestorationEmail(string email, string guid)
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

            string msgBody = string.Format(@"Dear customer, <br/>
            Please follow this link to restore your password: {0}/Auth/RestorePassword?guid={1} 
            <br/><br/> 
            Kind regards, <br/> Internet Shop", "http://localhost:13384", guid);


            MailMessage message = new MailMessage(msgFrom, email, "Internet Shop - Password Restoration", msgBody);

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