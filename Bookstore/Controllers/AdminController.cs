using System;
using Bookstore.Models;
using Bookstore.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Controllers
{
    public class UpdateUserDto
    {
       [FromForm(Name = "id")]
       public long Id { get; set; }
       
       [FromForm(Name = "username")]
       public string UserName { get; set; }
       
       [FromForm(Name = "password")]
       public string Password { get; set; }
       
       [FromForm(Name = "confirm-password")]
       public string ConfirmPassword { get; set; }
    }
    
    public class CreateUserDto
    {
       [FromForm(Name = "username")]
       public string UserName { get; set; }
       
       [FromForm(Name = "password")]
       public string Password { get; set; }
       
       [FromForm(Name = "confirm-password")]
       public string ConfirmPassword { get; set; }
       
       [FromForm(Name = "is-admin")]
       public bool IsAdmin { get; set; }
    }
    
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
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }
        
        [HttpPost]
        public IActionResult Create(CreateUserDto create)
        {
            if (create.Password != create.ConfirmPassword)
            {
                ViewData["Message"] = "Password confirmation does not match";
                return View();
            }

            if (_service.GetUserByUserName(create.UserName) != null)
            {
                ViewData["Message"] = "A user with this name already exists";
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
            if (update.Password != update.ConfirmPassword)
            {
                ViewData["User"] = _service.GetUserById(update.Id);
                ViewData["Message"] = "Password confirmation does not match";
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