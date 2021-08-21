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
using Microsoft.AspNetCore.Mvc.RazorPages;
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

        [HttpGet]
        public IActionResult Index(string? search, int page = 1)
        {
            page = Math.Max(1, page);
            Settings settings =_bookstore.GetUserSettings();
            // if (HttpContext.Request.Query.Keys.Count == 0 && settings?.DefaultQuery != null)
            // {
            //     var qparams = new RouteValueDictionary() { { "search", settings?.DefaultQuery } };
            //     return RedirectToAction(nameof(Index), "Bookstore", qparams);
            // }
            SearchQueryResult searchQuery = new(new SearchQuery(search), page, settings.DefaultPaginationLimit);
            searchQuery.Execute(_bookstore);
            
            ViewData["Search"] = searchQuery;
            ViewData["Settings"] = settings;
            ViewData["PageNumber"] = page;
            
            int pageCount = searchQuery.TotalQueriedBookmarks / settings.DefaultPaginationLimit;
            bool evenPageCount = searchQuery.TotalQueriedBookmarks % settings.DefaultPaginationLimit == 0;
            ViewData["MaxPageNumber"] = pageCount + (evenPageCount ? 0 : 1);
            
            return View();
        }
        
        [HttpPost]
        public IActionResult Index(string action, long[] selected)
        {
            if (action == "Edit")
            {
                return RedirectToAction("Edit", "Bookmarks",new RouteValueDictionary() {{"ids", selected}});
            }

            if (action == "Delete")
            {
                var bookmarksToDelete = _bookstore.QueryUserBookmarksByIds(selected);
                _context.Bookmarks.RemoveRange(bookmarksToDelete);
                _bookstore.RefreshTagsAndFolders();
                
                _context.SaveChanges();
                return Index(null);
            }

            if (action == "Archive")
            {
                var bookmarksToArchive = _bookstore.QueryUserBookmarksByIds(selected);
                var archiver = new BookmarkArchiver(_bookstore);
                archiver.ArchiveAll(bookmarksToArchive);

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