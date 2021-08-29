using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Bookstore.Models;
using Bookstore.Models.View;
using Bookstore.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

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
        public IActionResult Create([FromQuery] string  message, [FromQuery] string error)
        {
            ViewData["Settings"] = _bookstore.GetUserSettings();
            ViewData["Message"] = message;
            ViewData["Error"] = error;
            return View();
        }

        [HttpGet]
        public FileContentResult Favicon(long id)
        {
            // User user = _context.GetUser(User).Include(u => u.Bookmarks).First();
            // Bookmark bookmark = user.Bookmarks.Single(bm => bm.Id == id);

            Bookmark? bookmark = _bookstore.GetSingleBookmarkById(id);
            byte[] icon = bookmark?.Favicon ?? new byte[]{};
            string iconMimeType = bookmark?.FaviconMime ?? "";
            var result = new FileContentResult(icon, iconMimeType);
            
            Response.Headers.Add("Cache-Control", "public, immutable");
            Response.Headers.Add("Expires", "Fri, 01 Jan 2500 00:00:00 +0000");
            Response.Headers.Add("Content-Length", $"{icon.Length}");
            
            return result;
        }
        
        [HttpPost]
        public IActionResult Save([FromForm(Name = "url")] string? urlString, [FromForm(Name = "archive-bookmark")] bool archiveBookmark, [FromQuery] bool editRedirect)
        {
            var routeValues = new RouteValueDictionary();
            
            if (string.IsNullOrWhiteSpace(urlString))
            {
                routeValues["Error"] = "Bookmark URL cannot be empty";
                return RedirectToAction("Create", "Bookmarks", routeValues);
            }
            
            if (!urlString.StartsWith("http://") && !urlString.StartsWith("https://"))
            {
                routeValues["Error"] = "Bookmark URL must start with http:// or https://";
                return RedirectToAction("Create", "Bookmarks", routeValues);
            }

            var url = new Uri(urlString);
            bool bookmarkExists = _bookstore.QueryAllUserBookmarks().Any(bm => bm.Url == url);

            if (bookmarkExists)
            {
                routeValues["Error"] = $@"Bookmark with URL ""{urlString}"" already exists";
                return RedirectToAction("Create", "Bookmarks", routeValues);
            }
                
            var load = BookmarkLoader.Create(url, _httpClientFactory.CreateClient());
            
            _context.Bookmarks.Add(new Bookmark()
            {
                Archive = null, // TODO: Get based on form setting if this is enabled?
                UserId = _bookstore.User.Id,
                User = _bookstore.User,
                Created = DateTime.Now,
                Modified = DateTime.Now,
                Favicon = load.Favicon,
                FaviconMime = load.FaviconMimeType,
                Folder = null,
                Tags = new HashSet<Tag>(),
                Title = load.Title,
                Url = url,
                ArchiveId = null,
                FolderId = null
            });

            _context.SaveChanges();

            if (editRedirect)
            {
                // Now get the max bookmark ID for this user so we can fetch the URL
                routeValues["ids"] = _bookstore.QueryAllUserBookmarks().OrderByDescending(b => b.Id).First().Id;
                return RedirectToAction(nameof(Edit), "Bookmarks", routeValues);
            }
            else
            {
                return RedirectToAction(nameof(Index), "Bookstore");
            }
        }
        
        [HttpGet]
        public IActionResult Edit(long[] ids)
        {
            var bookmarks = _bookstore.QueryUserBookmarksByIds(ids).ToList();

            ViewData["Folders"] = _bookstore.QueryAllUserFolders()
                .ToList()
                .OrderBy(f => f.ToMenuString())
                .ToList();
            
            ViewData["Bookmarks"] = bookmarks;
            return View();
        }

        public class BookmarkEditDto
        {
            public Uri Url { get; set; }
            public string Title { get; set; }
            public string? Tags { get; set; }
            public long? Folder { get; set; }
            public string? CustomFolder { get; set; } // Only if user chooses to create a new folder
        }
        
        private void EditBookmark(long id, BookmarkEditDto upload, BookstoreResolver resolver, Folder? folder, bool singleEdit)
        {
            Bookmark? bookmark = _bookstore.GetSingleBookmarkById(id);

            if (bookmark == null)
                throw new ArgumentException($"Unable to find bookmark with ID = {id}");
            
            bookmark.Modified = DateTime.Now;
            bookmark.Folder = folder;
            
            if (singleEdit)
            {
                bookmark.Title = upload.Title;
                bookmark.Url = bookmark.Url;
            }
            
            HashSet<string> tagNames = upload.Tags
                ?.Split(",")
                .Select(t => t.Trim().ToLower())
                .ToHashSet() ?? new HashSet<string>();

            // Clear previous tags
            bookmark.Tags = resolver.ResolveTags(tagNames);
        }

        [HttpGet]
        public IActionResult Archive(long id)
        {
            Archive? archive = _bookstore.GetArchiveByBookmarkId(id);

            if (archive == null)
                throw new ArgumentException($"Invalid bookmark ID: {id}");
            
            Response.Headers.Add("Cache-Control", "public, immutable");
            Response.Headers.Add("Expires", "Fri, 01 Jan 2500 00:00:00 +0000");
            Response.Headers.Add("Content-Length", $"{archive.Bytes.Length}");
            Response.Headers.Add("Content-Type", $"{archive.Mime}");

            byte[] content = archive.Formatted ?? archive.Bytes;
            
            return new FileContentResult(content, archive.Mime);
        }
        
        [HttpPost]
        public IActionResult Edit([FromForm] long[] id, [FromForm] BookmarkEditDto upload)
        {
            bool singleEdit = id.Length == 1;
            try
            {
                if (upload.Folder is not null && !_bookstore.QueryAllUserFolders().Any(f => f.Id == upload.Folder))
                {
                    throw new ArgumentException($"Folder with ID {upload.Folder} does not exist");
                }

                BookstoreResolver resolver = new(_bookstore);
                Folder? folder = null;
                if (!string.IsNullOrWhiteSpace(upload.CustomFolder))
                {
                    string[] folderText = upload.CustomFolder.Trim('/').Split('/');
                    folder = resolver.ResolveFolder(folderText);
                }
                else
                {
                    // By default, set selected folder as this bookmarks folder
                    if (upload.Folder.HasValue)
                    {
                        folder = _bookstore.QueryAllUserFolders().FirstOrDefault(f => f.Id == (long)upload.Folder);
                    }
                }
                
                foreach (long bookmarkId in id)
                {
                    EditBookmark(bookmarkId, upload, resolver, folder, singleEdit);
                }
            }
            catch (Exception e)
            {
                return View("Error", new ErrorViewModel(e.Message, nameof(BookmarksController), nameof(Edit)) );
            }
            
            _bookstore.CleanupBookmarkOrphans();
            _context.SaveChanges();
            return RedirectToAction(nameof(Index), "Bookstore");
        }
    }
}