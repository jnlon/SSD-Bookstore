using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using Bookstore.Constants.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Bookstore.Models;
using Bookstore.Models.View;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using Bookstore.Utilities;
using Microsoft.AspNetCore.Routing;

namespace Bookstore.Controllers
{
    [Authorize(Policy = "MemberOnly")]
    public class BookstoreController : Controller
    {
        private readonly ILogger<BookstoreController> _logger;
        private readonly BookmarksContext _context;
        private readonly BookstoreService _bookstore;

        public BookstoreController(ILogger<BookstoreController> logger, BookmarksContext context, BookstoreService bookstore)
        {
            _logger = logger;
            _context = context;
            _bookstore = bookstore;
        }

        private List<Bookmark> QueryBookmarks(SearchQuery search, int page, int pageCount)
        {
            Func<Bookmark, IComparable> orderby = search.SortField switch
            {
                SearchQueryField.Archived => bm => bm.Archive == null,
                SearchQueryField.Folder => bm => bm.Folder?.ToMenuString() ?? string.Empty,
                SearchQueryField.Tag => bm => string.Join(",", bm.Tags.ToList().OrderBy(tag => tag.Name)),
                SearchQueryField.Title => bm => bm.Title,
                SearchQueryField.Url => bm => bm.Url.ToString()
            };

            List<Bookmark> allBookmarks = _bookstore.QueryAllUserBookmarks().ToList();
            
            IEnumerable<Bookmark> query = allBookmarks.Where(search.PassesAllFilters);

            // Apply sort on selected field
            if (search.SortDescending)
                query = query.OrderByDescending(orderby);
            else
                query = query.OrderBy(orderby);

            return query.ToList();
        }

        [HttpGet]
        public IActionResult Index(string? search)
        {
            var searchQuery = new SearchQuery(search);
            ViewData["Search"] = searchQuery;
            ViewData["Folders"] = _bookstore.QueryAllUserFolders().ToList();
            ViewData["Bookmarks"] = QueryBookmarks(searchQuery, 1, 500);
            
            return View();
        }
        
        // TODO: Move me somehere better!
        private bool CanDeleteFolder(Folder folder, List<Bookmark> bookmarksToDelete, List<Folder> allFolders)
        {
            bool folderBookmarksWillBeDeleted = folder
                .Bookmarks
                .All(bm => bookmarksToDelete.Any(tdl => tdl.Id == bm.Id));
            
            if (folderBookmarksWillBeDeleted)
            {
                // All the child folders also contain only bookmarks to be deleted
                return allFolders
                    .Where(f => f.ParentId == folder.Id)
                    .All(f => CanDeleteFolder(f, bookmarksToDelete, allFolders));
            }

            return false;
        }
        
        [HttpPost]
        public /*string*/ IActionResult Index(string action, ulong[] selected)
        {
            if (action == "Edit" && selected.Length == 1)
            {
                return RedirectToAction("Edit", "Bookmarks",new RouteValueDictionary() {{"id", selected[0]}});
            }

            if (action == "Delete")
            {
                // TODO: We need to store the functionality "re-scan library and delete folders/tags not currently used" somewhere
                // because we also need this to occur when saving/editing existing bookmarks

                var allFolders = _bookstore
                    .QueryAllUserFolders()
                    .Include(f => f.Bookmarks)
                    .ToList();

                var bookmarksToDelete = _bookstore
                    .QueryAllUserBookmarks()
                    .ToList()
                    .Where(bm => selected.Contains(bm.Id))
                    .ToList();

                // Now we need to make sure 2 conditions are met before we attempt deleting the folder:
                // - If any bookmarks exist, they are are going to be deleted
                // - If any child folders exist, it contains no bookmarks or only bookmarks to be deleted
                
                var foldersToDelete = allFolders
                    .Where(f => CanDeleteFolder(f, bookmarksToDelete, allFolders))
                    .ToList();
                
                var tagsToDelete = _bookstore
                    .QueryAllUserTags()
                    .Include(tag => tag.Bookmarks)
                    .ToList()
                    .Where(tag => tag.Bookmarks.All(bm => bookmarksToDelete.Any(bmtd => bmtd.Id == bm.Id)));
                
                _context.Bookmarks.RemoveRange(bookmarksToDelete);
                _context.Folders.RemoveRange(foldersToDelete);
                _context.Tags.RemoveRange(tagsToDelete);
                _context.SaveChanges();
                return Index(null);
            }

            return View("Error", new ErrorViewModel($"Invalid Action: {action}", nameof(BookstoreController), nameof(Index)));
            //return $"Action = {action}, selected = {string.Join(",", selected)}";
        }
        
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(); //new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
        }
    }
}