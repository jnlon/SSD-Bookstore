using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Bookstore.Controllers.Helpers;
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
        private readonly BookstoreService _bookstore;

        public BookmarksController(IHttpClientFactory clientFactory, BookmarksContext context, BookstoreService bookstore)
        {
            _httpClientFactory = clientFactory;
            _context = context;
            _bookstore = bookstore;
        }
        
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpGet]
        public FileContentResult Favicon(ulong id)
        {
            // User user = _context.GetUser(User).Include(u => u.Bookmarks).First();
            // Bookmark bookmark = user.Bookmarks.Single(bm => bm.Id == id);

            Bookmark? bookmark = _bookstore.QuerySingleBookmarkById(id);
            byte[] icon = bookmark?.Favicon ?? new byte[]{};
            string iconMimeType = bookmark?.FaviconMime ?? "";
            var result = new FileContentResult(icon, iconMimeType);
            
            Response.Headers.Add("Cache-Control", "public, immutable");
            Response.Headers.Add("Expires", "Fri, 01 Jan 2500 00:00:00 +0000");
            Response.Headers.Add("Content-Length", $"{icon.Length}");
            
            return result;
        }
        
        [HttpPost]
        public IActionResult LoadAndEdit(Uri url)
        {
            var load = BookmarkLoader.Create(url, _httpClientFactory.CreateClient());
            
            _context.Bookmarks.Add(new Bookmark()
            {
                Archive = null, // TODO: Get based on form setting if this is enabled?
                UserId = _bookstore.User.Id,
                Created = DateTime.Now,
                Modified = DateTime.Now,
                Favicon = load.Favicon,
                FaviconMime = load.FaviconMimeType,
                Folder = null,
                Tags = new HashSet<Tag>(),
                Title = load.Title,
                Url = url
            });

            _context.SaveChanges();
            
            // Now get the max bookmark ID for this user so we can fetch the URL

            var bookmark = _bookstore
                .QueryAllUserBookmarks() //user.Bookmarks
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
            Bookmark? bookmark = _bookstore.QuerySingleBookmarkById(id);

            if (bookmark == null)
                return View("Error", new ErrorViewModel($"Unable to find bookmark with ID = {id}", nameof(BookmarksController), nameof(Edit)) );

            ViewData["Folders"] = _bookstore.QueryAllUserFolders();
            ViewData["Bookmark"] = bookmark;
            return View();
        }

        public class BookmarkEditDto
        {
            public Uri Url { get; set; }
            public string Title { get; set; }
            public string? Tags { get; set; }
            public ulong? Folder { get; set; }
            public string? NewFolder { get; set; } // Only if user chooses to create a new folder
        }

        [HttpPost]
        public IActionResult Edit([FromRoute] ulong id, [FromForm] BookmarkEditDto upload)
        {
            // var user = _context.GetUser(User)
            //     .Include(u => u.Bookmarks)
            //     .ThenInclude(bm => bm.Tags)
            //     .First();

            Bookmark? bookmark = _bookstore.QuerySingleBookmarkById(id); // .First(b => b.Id == id);
            
            if (bookmark == null)
                return View("Error", new ErrorViewModel($"Unable to find bookmark with ID = {id}", nameof(BookmarksController), nameof(Edit)) );
            
            bookmark.Modified = DateTime.Now;
            bookmark.Title = upload.Title;
            bookmark.Url = bookmark.Url;
            
            // Clear previous tags
            bookmark.Tags.Clear();
            
            // Add tags from upload to bookmark entity, re-using tags on users account if they exist
            var th = new TagHelper(_context.Tags, _bookstore.User.Id);
            foreach (string tagName in th.ParseTagList(upload.Tags ?? string.Empty))
                bookmark.Tags.Add(th.GetOrCreateNewTag(tagName));

            // 1. if the folder ID not-null and does not exist in folder list:
            //  - Throw an error
            // 2. If the new folder string is specified:
            //  - create that folder
            //  - set the bookmark folder to this new object
            // 3. Else:
            //  - Set folder equal to uploaded folder

            var fh = new FolderHelper(_context.Folders, _bookstore.User.Id);

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
            return RedirectToAction(nameof(Index), "Bookstore");
        }
    }
}