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
using System.Threading.Tasks;
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
            SearchQuery searchQuery = new(search, _bookstore);
            SearchQueryResult searchResult = new(searchQuery, page, settings.DefaultPaginationLimit);
            searchResult.Execute(_bookstore);
            
            ViewData["Search"] = searchResult;
            ViewData["Settings"] = settings;
            ViewData["PageNumber"] = page;
            
            int pageCount = searchResult.TotalQueriedBookmarks / settings.DefaultPaginationLimit;
            bool evenPageCount = searchResult.TotalQueriedBookmarks % settings.DefaultPaginationLimit == 0;
            ViewData["MaxPageNumber"] = pageCount + (evenPageCount ? 0 : 1);
            
            return View();
        }
        
        [HttpPost]
        public IActionResult Index(string action, long[] selected, string? search, int page = 1)
        {
            if (action == "Edit")
            {
                return RedirectToAction("Edit", "Bookmarks",new RouteValueDictionary() {{"ids", selected}});
            } 
            else if (action == "Delete")
            {
                var bookmarksToDelete = _bookstore.QueryUserBookmarksByIds(selected);
                _context.Bookmarks.RemoveRange(bookmarksToDelete);
                _bookstore.RefreshTagsAndFolders();
                _context.SaveChanges();
            }
            else if (action == "Archive")
            {
                var bookmarksToArchive = _bookstore.QueryUserBookmarksByIds(selected);
                var archiver = new BookmarkArchiver();
                archiver.ArchiveAll(bookmarksToArchive);
                _context.SaveChanges();
            }
            else
            {
                return View("Error", new ErrorViewModel($"Invalid Action: {action}", nameof(BookstoreController), nameof(Index)));
            }
            
            var qparams = new RouteValueDictionary { { "search", search }, {"page", page} };
            return RedirectToAction(nameof(Index), "Bookstore", qparams);
        }
        
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(); //new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
        }
    }
}