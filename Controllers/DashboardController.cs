using Microsoft.AspNetCore.Mvc;

namespace AuthSystem.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Dashboard()
        {
            if(HttpContext.Session.GetString("Username") == null)
            {
                return RedirectToAction("Login", "Authentication");
            }
            return View();

        }
    }
}
