using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Client.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Index()
        {
            return Login();
        }

        public IActionResult Login()
        {
            return View();
        }
    }
}
