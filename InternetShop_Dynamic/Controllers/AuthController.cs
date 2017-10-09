using InternetShop_Dynamic.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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
    }
}