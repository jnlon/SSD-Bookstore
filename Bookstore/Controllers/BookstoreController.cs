using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Bookstore.Common;
using Bookstore.Models;
using Bookstore.Models.View;
using Bookstore.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Bookstore.Controllers
{
    [Authorize(Policy = "MemberOnly")]
    public class BookstoreController : Controller
    {
        private readonly ILogger<BookstoreController> _logger;
        private readonly BookmarksContext _context;
        private readonly BookstoreService _bookstore;
        private readonly HttpClient _httpClient;

        public BookstoreController(IHttpClientFactory clientFactory, ILogger<BookstoreController> logger, BookmarksContext context, BookstoreService bookstore)
        {
            _logger = logger;
            _context = context;
            _bookstore = bookstore;
            _httpClient = clientFactory.CreateClient(Constants.AppName);
        }

        [HttpGet]
        public IActionResult Index(string? search, int page = 1)
        {
            page = Math.Max(1, page);
            Settings settings =_bookstore.GetUserSettings();
            if (HttpContext.Request.Query.Keys.Count == 0 && !string.IsNullOrWhiteSpace(settings.DefaultQuery))
            {
                var routeValues = new RouteValueDictionary { { "search", settings.DefaultQuery } };
                return RedirectToAction(nameof(Index), "Bookstore", routeValues);
            }
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
        public async Task<IActionResult> Index(string action, long[] selected, string? search, int page = 1)
        {
            if (action == "Edit")
            {
                return RedirectToAction("Edit", "Bookmarks",new RouteValueDictionary() {{"ids", selected}});
            } 
            else if (action == "Refresh")
            {
                var bookmarksToRefresh = _bookstore.QueryUserBookmarksByIds(selected).AsEnumerable().ToList();
                var downloadTasks = bookmarksToRefresh.Select(bm => BookmarkLoader.Create(bm.Url, _httpClient)).ToList();
                    
                while (downloadTasks.Any())
                {
                    Task<BookmarkLoader> finishedTask = await Task.WhenAny(downloadTasks);
                    downloadTasks.Remove(finishedTask);
                    
                    var loader = await finishedTask;
                    Bookmark bookmark = bookmarksToRefresh.First(bm => bm.Url == loader.OriginalUrl);
                    bookmark.Favicon = loader.Favicon;
                    bookmark.Title = loader.Title;
                    bookmark.Url = loader.FinalUrl;
                    bookmark.Modified = DateTime.Now;
                }

                await _context.SaveChangesAsync();
            }
            else if (action == "Delete")
            {
                var bookmarksToDelete = _bookstore.QueryUserBookmarksByIds(selected);
                _context.Bookmarks.RemoveRange(bookmarksToDelete);
                _bookstore.CleanupBookmarkOrphans();
                await _context.SaveChangesAsync();
            }
            else if (action == "Archive")
            {
                var bookmarksToArchive = _bookstore.QueryUserBookmarksByIds(selected);
                var archiver = new BookmarkArchiver(_httpClient);
                await archiver.ArchiveAll(bookmarksToArchive);
                await _context.SaveChangesAsync();
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