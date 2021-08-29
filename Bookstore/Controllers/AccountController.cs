#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Bookstore.Constants.Authorization;
using Bookstore.Models;
using Bookstore.Utilities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Controllers
{
    public class AccountController : Controller
    {
        private BookmarksContext _context;

        public AccountController(BookmarksContext context)
        {
            _context = context;
        }
            
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string? username, string? password)
        {
            if (username is not null && password is not null)
            {
                User? user = _context.Users.FirstOrDefault(u => u.Username == username);
                if (user is not null && ValidateLogin(user, password))
                {
                    var claims = new List<Claim>
                    {
                        new(BookstoreClaims.UserId, user.Id.ToString()),
                        new(BookstoreClaims.UserName, username),
                        new(BookstoreClaims.Role, user.Admin ? BookstoreRoles.Admin : BookstoreRoles.Member)
                    };

                    // Create cookie and add it to current response
                    await HttpContext.SignInAsync(new ClaimsPrincipal(new ClaimsIdentity(claims, "Cookies", "user", "role")));
                    
                    return Redirect("/");
                }
            }
            ViewData["Error"] = "Login Unsuccessful";
            return View();
        }

        public IActionResult AccessDenied(string returnUrl = null)
        {
            return View();
        }
        
        private bool ValidateLogin(User user, string password)
        {
            return Crypto.PasswordHashMatches(password, user.PasswordHash, user.PasswordSalt);
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return Redirect("/");
        }
    }
}