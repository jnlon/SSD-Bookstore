using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Security.Cryptography.Xml;
using Bookstore.Constants.Authorization;
using Bookstore.Models;
using Bookstore.Models.View;
using Bookstore.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Controllers
{
    [Authorize(Policy = "MemberOnly")]
    public class BookmarksController : Controller
    {
        private IHttpClientFactory _httpClientFactory;
        private readonly BookmarksContext _context;

        public BookmarksController(IHttpClientFactory clientFactory, BookmarksContext context)
        {
            _httpClientFactory = clientFactory;
            _context = context;
        }
        
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpGet]
        public FileContentResult Favicon(ulong id)
        {
            var user = _context.GetUser(User).Include(u => u.Bookmarks).First();
            var icon = user.Bookmarks.Single(bm => bm.Id == id).Favicon;
            Response.Headers.Add("Cache-Control", "public, immutable");
            Response.Headers.Add("Expires", "Fri, 01 Jan 2500 00:00:00 +0000");
            Response.Headers.Add("Content-Length", $"{icon.Length}");
            var result = new FileContentResult(icon, "image/png");
            return result;
        }
        
        [HttpPost]
        public IActionResult LoadAndEdit(Uri url)
        {
            var load = BookmarkLoader.Create(url, _httpClientFactory.CreateClient());
            var user = _context.GetUser(User).Include(u => u.Bookmarks).First();
            
            user.Bookmarks.Add(new Bookmark()
            {
                Archive = null, // TODO: Get based on form setting if this is enabled?
                Created = DateTime.Now,
                Modified = DateTime.Now,
                Favicon = load.Favicon,
                Folder = null,
                Tags = new Collection<Tag>(),
                Title = load.Title,
                Url = url
            });

            _context.SaveChanges();
            
            // Now get the max bookmark ID for this user so we can fetch the URL

            var bookmark = user.Bookmarks
                .OrderByDescending(b => b.Id)
                .First();

            return RedirectToAction(nameof(Edit), "Bookmarks", new { id = bookmark.Id });
        }
        
        [HttpPost]
        public string Save(string url)
        {
            return $"save url: {url}";
        }
        
        [HttpGet]
        public IActionResult Edit(ulong id)
        {
            var user = _context
                .GetUser(User)
                .Include(u => u.Bookmarks)
                .First();
            
            Bookmark? bookmark =
                user
                .Bookmarks
                .Find(b => b.Id == id);

            if (bookmark == null)
                return View("Error", new ErrorViewModel($"Unable to find bookmark with ID = {id}", nameof(BookmarksController), nameof(Edit)) );

            var folders = _context
                .Folders
                .Where(f => f.UserId == user.Id)
                .ToList();
            
            ViewData["Folders"] = folders;
            ViewData["Bookmark"] = bookmark;
            return View();
        }

        public class BookmarkEditDto
        {
            public Uri Url { get; set; }
            public string Title { get; set; }
            public string Tags { get; set; }
            public ulong? Folder { get; set; }
            public string? NewFolder { get; set; } // Only if user chooses to create a new folder
        }

        private class FolderHelper
        {
            private DbSet<Folder> _folders;
            private ulong _userId;
            public FolderHelper(DbSet<Folder> folders, ulong userId)
            {
                _folders = folders;
                _userId = userId;
            }

            public Folder? GetFolder(ulong folderId) => _folders.FirstOrDefault(f => f.UserId == _userId && f.Id == folderId);
            // A valid folder is null or exists for this user
            public bool ValidFolder(ulong? folderId) => folderId == null || GetFolder((ulong) folderId) != null;
            public Folder CreateFolder(string name, Folder? parent)
            {
                var folder = new Folder()
                {
                    Name = name,
                    UserId = _userId,
                    Parent = parent
                };
                _folders.Add(folder);
                return folder;
            }
        }

        [HttpPost]
        public IActionResult Edit([FromRoute] ulong id, [FromForm] BookmarkEditDto upload)
        {
            var user = _context.GetUser(User).Include(u => u.Bookmarks).First();
            var bookmark = user.Bookmarks.First(b => b.Id == id);
            bookmark.Modified = DateTime.Now;
            bookmark.Title = upload.Title;
            bookmark.Url = bookmark.Url;
            
            // 1. if the folder ID not-null and does not exist in folder list:
            //  - Throw an error
            // 2. If the new folder string is specified:
            //  - create that folder
            //  - set the bookmark folder to this new object
            // 3. Else:
            //  - Set folder equal to uploaded folder

            var fh = new FolderHelper(_context.Folders, user.Id);

            // Show error if the selected folder ID is provided but invalid (wrong user? stale entry? hack attempt?)
            if (!fh.ValidFolder(upload.Folder))
                return View("Error", new ErrorViewModel($"Folder with ID {id} does not exist", nameof(BookmarksController), nameof(Edit)) );

            // By default, set selected folder as this bookmarks folder
            if (upload.Folder is null)
                bookmark.Folder = null;
            else
                bookmark.Folder = fh.GetFolder((ulong)upload.Folder);

            // If a "new folder" string was given, treat the "Folder" drop-down selected item as the parent
            if (upload.NewFolder is not null)
            {
                Folder? parent = bookmark.Folder;
                bookmark.Folder = fh.CreateFolder(upload.NewFolder, parent);
            }
            
            // TODO: Update bookmark folder ID?
            _context.SaveChanges();
            return RedirectToAction(nameof(Index), "Home");
        }

        // Use query param ids=1,2,3... to get editable bookmarks
        //public IActionResult Edit()
        //{
        //    return View();
        //}
        
        ///////////////////////////////////////////
        // Non-Route Logic
        ///////////////////////////////////////////
    }
}