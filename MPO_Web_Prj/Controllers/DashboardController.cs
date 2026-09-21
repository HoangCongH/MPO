using Microsoft.AspNetCore.Mvc;
using MPO_Web_Prj.Data;

namespace MPO_Web_Prj.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult SwitchMode(string mode, string? returnUrl)
        {
            if (mode != "u01" && mode != "pro")
            {
                return BadRequest();
            }

            Response.Cookies.Append(ReportMode.CookieName, mode, new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps,
                Expires = DateTimeOffset.UtcNow.AddYears(1)
            });

            return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action("Index", "Dashboard")!);
        }
    }
}
