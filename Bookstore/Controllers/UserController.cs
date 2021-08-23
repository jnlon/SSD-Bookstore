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

namespace Bookstore.Controllers
{
    public enum ImportFileFormat
    {
        Netscape,
        CSV
    }

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

        private void ImportNetscapeBookmarks(Stream stream)
        {
            //this.User
            var importer = new NetscapeImporter(_context, _bookstore);
            importer.Import(stream);
        }

        private void ImportCsvBookmarks(Stream stream)
        {
            // TODO: Use CSV Helper here
        }

        [HttpPost]
        public IActionResult ImportBookmarks([FromForm] ImportFileFormat format, [FromForm] IFormFile content)
        {
            // TODO: Handle case where file upload is null (treat as empty string?)
            using var stream = content.OpenReadStream();

            if (format == ImportFileFormat.Netscape)
                ImportNetscapeBookmarks(stream);
            else if (format == ImportFileFormat.CSV)
                ImportCsvBookmarks(stream);
            
            return RedirectToAction(nameof(Settings));
        }
    }
}