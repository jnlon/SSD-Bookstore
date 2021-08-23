using System.Security.Claims;
using Bookstore.Constants.Authorization;
using Bookstore.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Controllers
{
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
    }
}