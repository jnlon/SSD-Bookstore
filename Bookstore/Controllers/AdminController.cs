using System;
using Bookstore.Controllers.Dto;
using Bookstore.Models;
using Bookstore.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Controllers
{
    // Controller responsible for logic and views when logged in as an admin account
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
        
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }
        
        [HttpPost]
        public IActionResult Create(CreateUserDto create)
        {
            if (_service.GetUserByUserName(create.UserName) != null)
            {
                ViewData["Error"] = "A user with this name already exists";
                return View();
            }
            
            if (!_service.ValidateNewPassword(create.Password, create.ConfirmPassword, out string? error))
            {
                ViewData["Error"] = error;
                return View();
            }
            
            _service.CreateNewUser(create.UserName, create.Password, create.IsAdmin);
            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }
        
        [HttpGet]
        public IActionResult Edit(long id)
        {
            ViewData["User"] = _service.GetUserById(id);
            return View();
        }
        
        [HttpPost]
        public IActionResult Edit(UpdateUserDto update)
        {
            if (!_service.ValidateNewPassword(update.Password, update.ConfirmPassword, out string? error))
            {
                ViewData["User"] = _service.GetUserById(update.Id);
                ViewData["Error"] = error;
                return View();
            }
            
            _service.UpdateUserCredentials(update.Id, update.UserName, update.Password);
            _context.SaveChanges();
            
            return RedirectToAction(nameof(Index));
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