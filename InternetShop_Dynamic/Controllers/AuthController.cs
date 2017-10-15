using InternetShop_Dynamic.Models;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
                        return RedirectToAction("Index", "Main");
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

        public ActionResult RestorePassword()
        {
            ChangePasswordBindingModel model = new ChangePasswordBindingModel();
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
    }
}