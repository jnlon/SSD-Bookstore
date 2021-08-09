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
    
    [Authorize(Policy = "MemberOnly")]
    public class UserController : Controller
    {
        private readonly BookmarksContext _context;

        public UserController(BookmarksContext context)
        {
            _context = context;
        }
        
        // GET
        public IActionResult Account()
        {
            return View();
        }
        
        public IActionResult Settings()
        {
            return View();
        }

        private void ImportNetscapeBookmarks(Stream stream)
        {
            //this.User
            var importer = new NetscapeImporter(_context, new List<Folder>(), User);
            importer.Import(stream);
        }

        private void ImportCsvBookmarks(Stream stream)
        {
            // TODO: Use CSV Helper here
        }

        [HttpPost]
        public string ImportBookmarks([FromForm] ImportFileFormat format, [FromForm] IFormFile content)
        {
            // TODO: Handle case where file upload is null (treat as empty string?)
            using var stream = content.OpenReadStream();

            if (format == ImportFileFormat.Netscape)
                ImportNetscapeBookmarks(stream);
            else if (format == ImportFileFormat.CSV)
                ImportCsvBookmarks(stream);
            
            return $"Format = {format} Content = ...";
        }
    }
}