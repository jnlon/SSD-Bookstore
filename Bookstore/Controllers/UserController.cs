using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Controllers
{
    [Authorize(Policy = "MemberOnly")]
    public class UserController : Controller
    {
        // GET
        public IActionResult Account()
        {
            return View();
        }
        
        public IActionResult Settings()
        {
            return View();
        }
    }
}