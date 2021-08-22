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
        public FileContentResult Favicon(long id)
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

            return RedirectToAction(nameof(Edit), "Bookmarks", new { ids = new long[]{bookmark.Id} });
        }
        
        [HttpPost]
        public string Save(string url)
        {
            return $"save url: {url}";
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
            public string? NewFolder { get; set; } // Only if user chooses to create a new folder
        }
        
        private void EditBookmark(long id, BookmarkEditDto upload, TagHelper th, Folder? folder, bool singleEdit)
        {
            Bookmark? bookmark = _bookstore.QuerySingleBookmarkById(id); // .First(b => b.Id == id);

            if (bookmark == null)
                throw new ArgumentException($"Unable to find bookmark with ID = {id}");
            
            bookmark.Modified = DateTime.Now;
            bookmark.Folder = folder;
            
            if (singleEdit)
            {
                bookmark.Title = upload.Title;
                bookmark.Url = bookmark.Url;
            }
            
            // Clear previous tags
            bookmark.Tags.Clear();
            
            // Add tags from upload to bookmark entity, re-using tags on users account if they exist
            foreach (string tagName in th.ParseTagList(upload.Tags ?? string.Empty))
                bookmark.Tags.Add(th.GetOrCreateNewTag(tagName));
        }

        [HttpGet]
        public IActionResult Archive(long id)
        {
            Bookmark? bookmark = _bookstore.QuerySingleBookmarkById(id);
            Archive? archive = bookmark?.Archive;

            if (bookmark == null || archive == null)
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
                var fh = new FolderHelper(_bookstore, _context);
                var th = new TagHelper(_bookstore, _context);
                
                if (!fh.ValidFolder(upload.Folder))
                    throw new ArgumentException($"Folder with ID {id} does not exist");

                // By default, set selected folder as this bookmarks folder
                Folder? folder = null;
                if (upload.Folder.HasValue)
                {
                    folder = fh.GetFolder((long)upload.Folder);
                }
                
                // If a "new folder" string was given, treat the "Folder" drop-down selected item as the parent
                if (upload.NewFolder is not null)
                {
                    Folder? parent = folder;
                    folder = fh.CreateFolder(upload.NewFolder, parent);
                }
                
                foreach (long bookmarkId in id)
                {
                    EditBookmark(bookmarkId, upload, th, folder, singleEdit);
                }
            }
            catch (Exception e)
            {
                return View("Error", new ErrorViewModel(e.Message, nameof(BookmarksController), nameof(Edit)) );
            }
            
            _bookstore.RefreshTagsAndFolders();
            _context.SaveChanges();
            return RedirectToAction(nameof(Index), "Bookstore");
        }
    }
}