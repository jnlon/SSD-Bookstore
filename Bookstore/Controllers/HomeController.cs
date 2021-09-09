using System.Net;
using Bookstore.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Controllers
{
    // Default index controller will simply redirect you to the appropriate controller/page
    // based on current auth status
    public class HomeController : Controller
    {
        public RedirectToActionResult Index()
        {
            if (User.IsBookstoreAdmin())
            {
                return new RedirectToActionResult(actionName: "Index", controllerName: "Admin", null);
            }
            
            if (User.IsBookstoreMember())
            {
                return new RedirectToActionResult(actionName: "Index", controllerName: "Bookstore", null);
            }
            
            // Unauthenticated!
            return new RedirectToActionResult(actionName: "Login", controllerName: "Account", null);
        }
        
        public IActionResult ErrorRedirect()
        {
            return RedirectToAction(nameof(Error));
        }

        // Generic error page for production deployments
        public IActionResult Error()
        {
            Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            return View();
        }
    }
}