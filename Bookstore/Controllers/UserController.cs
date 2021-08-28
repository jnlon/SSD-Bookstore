using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BookmarksManager;
using Bookstore.Models;
using Bookstore.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;

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
        public IActionResult Account()
        {
            return View();
        }
        
        [HttpGet]
        public IActionResult Settings()
        {
            ViewData["Settings"] = _bookstore.GetUserSettings() ?? new Settings();
            return View();
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
            return action switch
            {
                PortAction.Export => ExportBookmarks(format),
                PortAction.Import => ImportBookmarks(format, content),
                _ => throw new ArgumentException("Invalid settings action code: " + action)
            };
        }
        
        private IActionResult ExportBookmarks(PortFileFormat format)
        {
            ContentResult result = new();
            
            if (format == PortFileFormat.Netscape)
            {
                var exporter = new NetscapeExporter(_bookstore);
                result.Content = exporter.Export();
                result.ContentType = "text/html";
                Response.Headers.Add("Content-Disposition", $"attachment; filename=\"bookmarks_{DateTime.Now:yyyy-MM-d}.html\"");
            }
            else if (format == PortFileFormat.CSV)
            {
                var exporter = new CsvExporter(_bookstore);
                result.Content = exporter.Export();
                result.ContentType = "text/csv";
                Response.Headers.Add("Content-Disposition", $"attachment; filename=\"BookstoreExport_{DateTime.Now:yyyy-MM-d}.csv\"");
            }
            else
            {
                throw new ArgumentException("Invalid Format: " + format);
            }

            return result;
        }
        
        private IActionResult ImportBookmarks([FromForm] PortFileFormat format, [FromForm] IFormFile? content)
        {
            if (content == null)
                throw new ArgumentException("No file uploaded!");
            
            // TODO: Handle case where file upload is null (treat as empty string?)
            using var stream = content.OpenReadStream();

            if (format == PortFileFormat.Netscape)
            {
                var importer = new NetscapeImporter(_bookstore);
                importer.Import(stream);
            }
            else if (format == PortFileFormat.CSV)
            {
                var importer = new CsvImporter(_bookstore);
                importer.Import(stream);
            }
            else
            {
                throw new ArgumentException("Invalid Format: " + format);
            }
            
            _context.SaveChanges();
            
            return RedirectToAction(nameof(Settings));
        }
    }
}