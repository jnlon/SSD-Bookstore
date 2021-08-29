using System;
using System.Linq;
using Bookstore.Models;
using Bookstore.Utilities;
using CsvHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Bookstore.Controllers
{
    // Import or Export types
    public enum PortFileFormat { Netscape, CSV }

    // Action
    public enum PortAction { Import, Export }

    public class SettingsDto
    {
        [FromForm(Name = "default-query")]
        public string? DefaultQuery { get; set; }

        [FromForm(Name = "default-max-results")]
        public int? DefaultMaxResults { get; set; }

        [FromForm(Name = "archive-by-default")]
        public bool ArchiveByDefault { get; set; }
    }
    
    [Authorize(Policy = "MemberOnly")]
    public class UserController : Controller
    {
        private readonly BookmarksContext _context;
        private readonly BookstoreService _bookstore;

        public UserController(BookmarksContext context, BookstoreService bookstore)
        {
            _context = context;
            _bookstore = bookstore;
        }
        
        // GET
        [HttpGet]
        public IActionResult Account([FromQuery] string? error, [FromQuery] string? message)
        {
            ViewData["User"] = _bookstore.User;
            ViewData["Error"] = error;
            ViewData["Message"] = message;
            return View();
        }
        
        [HttpPost]
        public IActionResult Account([FromForm] UpdateUserDto update, [FromForm(Name = "current-password")] string currentPassword)
        {
            RouteValueDictionary routeValues = new();
                
            if (!_bookstore.ValidateUserPassword(currentPassword))
            {
                routeValues["Error"] = @"""Current password"" does not match password on account";
            }
            else if (update.Password != update.ConfirmPassword)
            {
                routeValues["Error"] = @"""New Password"" and ""Confirm New Password"" do not match";
            }
            else
            {
                routeValues["Message"] = "Password and username successfully updated";
                _bookstore.UpdateSelfCredentials(update.UserName, update.Password);
                _context.SaveChanges();
            }
            
            return RedirectToAction("Account", "User", routeValues);
        }
        
        [HttpGet]
        public IActionResult Settings([FromQuery] string? message, [FromQuery] string? error)
        {
            ViewData["Settings"] = _bookstore.GetUserSettings();
            ViewData["Error"] = error;
            ViewData["Message"] = message;
            return View("Settings");
        }
        
        [HttpPost]
        public IActionResult Settings(SettingsDto settings)
        {
            int paginationLimit = Math.Min(1000, Math.Max(1, settings.DefaultMaxResults ?? 100));
            string defaultQuery = settings.DefaultQuery ?? "";
            _bookstore.UpdateUserSettings(defaultQuery, settings.ArchiveByDefault, paginationLimit);
            _context.SaveChanges();
            return RedirectToAction(nameof(Settings));
        }

        [HttpPost]
        public IActionResult PortBookmarks(PortAction action, [FromForm] PortFileFormat format, [FromForm] IFormFile? content = null)
        {
            return (action, format) switch
            {
                (PortAction.Export, PortFileFormat.Netscape) => ExportBookmarks(new NetscapeExporter(_bookstore)),
                (PortAction.Export, PortFileFormat.CSV) =>      ExportBookmarks(new CsvExporter(_bookstore)),
                (PortAction.Import, PortFileFormat.Netscape) => ImportBookmarks(content, new NetscapeImporter(_bookstore)),
                (PortAction.Import, PortFileFormat.CSV)      => ImportBookmarks(content, new CsvImporter(_bookstore)),
                _ => throw new ArgumentException("Invalid settings action code: " + action)
            };
        }
        
        private IActionResult ExportBookmarks(IBookmarkExporter exporter)
        {
            ContentResult result = new();
            result.Content = exporter.Export();
            result.ContentType = exporter.ContentType;
            Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{exporter.FileName}\"");
            return result;
        }
        
        private IActionResult ImportBookmarks(IFormFile? content, IBookmarkImporter importer)
        {
            var routeValues = new RouteValueDictionary();
            if (content == null)
            {
                routeValues["Error"] = "Upload file missing, no bookmarks imported";
                return RedirectToAction(nameof(Settings), routeValues);
            }

            try
            {
                using var stream = content.OpenReadStream();
                int count = importer.Import(stream);
                _bookstore.CleanupBookmarkOrphans();
                _context.SaveChanges();
                routeValues["Message"] = $"Successfully imported {count} bookmarks";
            }
            catch (Exception e)
            {
                string message = e is CsvHelperException ? "CSV file is incorrectly formatted" : e.Message;
                routeValues["Error"] = $"Import process encountered an error: {message}";
            }
            
            return RedirectToAction(nameof(Settings), routeValues);
        }
    }
}