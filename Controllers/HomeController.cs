using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MultiTenantSample.Controllers
{

    /// <summary>
    /// This is our home controller. This controller does nothing useful in this 
    /// sample except to show up the default homepage at app startup.
    /// </summary>
    [Controller]
    public class HomeController : Controller
    {

        [AllowAnonymous, HttpGet("/")]
        public IActionResult Index()
        {
            return View("Homepage");
        }


        [Authorize, HttpGet("/login")]
        public IActionResult AuthenticatedIndex()
        {
            return View("Homepage");
        }

    }
}
