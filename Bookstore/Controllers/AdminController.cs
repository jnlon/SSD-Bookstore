using System;
using System.Linq;
using Bookstore.Constants.Authorization;
using Bookstore.Models;
using Bookstore.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SQLitePCL;

namespace Bookstore.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class AdminController : Controller
    {
        private BookstoreService _service;
        private BookmarksContext _context;
        public AdminController(BookmarksContext context, BookstoreService service)
        {
            _context = context;
            _service = service;
        }
        
        // GET
        public IActionResult Create()
        {
            return View();
        }
        
        public IActionResult Edit(long id)
        {
            return View();
        }
        
        public IActionResult Delete(long id)
        {
            if (id == _service.User.Id)
                throw new ArgumentException("Cannot delete own user");
            
            _service.DeleteUserById(id);
            _service.CleanupBookmarkOrphans();
            _context.SaveChanges();
            
            return RedirectToAction(nameof(Index));
        }
        
        public IActionResult Index()
        {
            ViewData["AdminStatistics"] = _service.GetAdminStatistics();
            ViewData["UserStatistics"] = _service.GetUserStatistics();
            ViewData["CurrentUser"] = _service.User;
            return View();
        }
    }
}