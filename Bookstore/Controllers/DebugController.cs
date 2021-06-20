using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Bookstore.Constants.Authorization;
using Bookstore.Models;
using Bookstore.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Bookstore.Controllers
{
    [EnvironmentFilter("Development")]
    public class DebugController : Controller
    {
        private BookmarksContext _context;
        public DebugController(BookmarksContext context)
        {
            _context = context;
        }
        
        // GET
        public IActionResult Index()
        {
            return View();
        }
        
        [AllowAnonymous]
        public string AddUser(string? username, string? password, bool isAdmin)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return "Username and password cannot be null";

            var (passwordHash, passwordSalt) = Crypto.GeneratePasswordHash(password);
            
            _context.Users.Add(new User()
            {
                Admin = isAdmin,
                Settings = new Settings()
                {
                    DefaultQuery = "Default"
                },
                Username = username,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                Bookmarks = new List<Bookmark>()
            });

            return _context.SaveChanges().ToString();
        }
        
        public string AddBookmark(Uri url, string? title)
        {
            if (string.IsNullOrWhiteSpace(url?.Host) || title is null)
                return "Title and URL required";
            
            var user = _context.GetUser(User).Include(u => u.Bookmarks).First();

            if (user is null)
                return "User is null?";
            
            // get root folder or create it
            user.Bookmarks.Add(new Bookmark()
            {
                Archive = null,
                Created = DateTime.Now,
                Modified = DateTime.Now,
                Tags = new HashSet<Tag>(),
                Favicon = new byte[] { },
                Title = title,
                Url = url,
                Folder = null
            });
            
            return _context.SaveChanges().ToString();
        }

        public string ListBookmarks()
        {
            var user = _context.GetUser(User).Include(u => u.Bookmarks).First();
            var urls = user.Bookmarks.Select(b => b.Url);
            return string.Join("\n", urls);
        }

        /*
        public IActionResult ViewUsers()
        {
            return new View();
        }
        
        public IActionResult ViewBookmarks()
        {
            return new View();
        }
        */
    }
}