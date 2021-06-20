using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using Bookstore.Constants.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Bookstore.Models;
using Bookstore.Models.View;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Bookstore.Controllers
{
    [Authorize(Policy = "MemberOnly")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly BookmarksContext _context;

        public HomeController(ILogger<HomeController> logger, BookmarksContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            ulong userId = ulong.Parse(User.FindFirstValue(Claims.UserId));
            ViewData["Folders"] = _context.Folders.Where(f => f.UserId == userId).ToList();
            ViewData["Bookmarks"] = _context
                .Users
                .Include(u => u.Bookmarks)
                .ThenInclude(bm => bm.Folder)
                .Include(u => u.Bookmarks)
                .ThenInclude(bm => bm.Tags)
                .Single(u => u.Id == userId)
                .Bookmarks;
            
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(); //new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
        }
    }
}