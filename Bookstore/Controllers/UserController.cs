using System;
using Bookstore.Controllers.Dto;
using Bookstore.Models;
using Bookstore.Utilities;
using CsvHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Bookstore.Controllers
{
    // Import or Export file types
    public enum PortFileFormat { Netscape, CSV }

    // Action: Import or Export
    public enum PortAction { Import, Export }

    // Controller responsible for managing user settings, changing self account passwords, importing and exporting bookmarks
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
        public IActionResult Settings(UpdateSettingsDto updateSettings)
        {
            var routeValues = new RouteValueDictionary();
            int paginationLimit = Math.Min(1000, Math.Max(1, updateSettings.DefaultMaxResults ?? 100));
            string defaultQuery = updateSettings.DefaultQuery ?? "";
            _bookstore.UpdateUserSettings(defaultQuery, updateSettings.ArchiveByDefault, paginationLimit);
            _context.SaveChanges();
            routeValues["Message"] = "Settings updated successfully";
            return RedirectToAction(nameof(Settings), routeValues);
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