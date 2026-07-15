using Microsoft.AspNetCore.Mvc;

namespace MPO_Web_Prj.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
