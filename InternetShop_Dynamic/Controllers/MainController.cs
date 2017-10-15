using InternetShop_Dynamic.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
        public ActionResult SimpleAccount()
        {
            return View();
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
    }
}