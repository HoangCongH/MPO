using Microsoft.AspNetCore.Mvc;

namespace MPO_Web_Prj.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult DataView()
        {
            return View();
        }

        public IActionResult RunTime()
        {
            return View();
        }

        public IActionResult StopTime()
        {
            return View();
        }
    }
}
