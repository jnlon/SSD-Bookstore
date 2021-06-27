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

namespace Bookstore.Controllers
{
    [Authorize(Policy = "MemberOnly")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly BookmarksContext _context;
        private ulong _userId;

        public HomeController(ILogger<HomeController> logger, BookmarksContext context)
        {
            _logger = logger;
            _context = context;
        }

        private List<Bookmark> QueryBookmarks(SearchQuery search, int page, int pageCount)
        {
            Func<Bookmark, IComparable> orderby = search.SortField switch
            {
                SearchQueryField.Archived => bm => bm.Archive == null,
                SearchQueryField.Folder => bm => bm.Folder?.ToMenuString() ?? string.Empty,
                SearchQueryField.Tag => bm => string.Join(",", bm.Tags.ToList().OrderBy(tag => tag)),
                SearchQueryField.Title => bm => bm.Title,
                SearchQueryField.Url => bm => bm.Url.ToString()
            };

           List<Bookmark> allBookmarks = _context
               .Users
               .Include(u => u.Bookmarks)
               .ThenInclude(bm => bm.Folder)
               .Include(u => u.Bookmarks)
               .ThenInclude(bm => bm.Tags)
               .Include(u => u.Bookmarks)
               .ThenInclude(bm => bm.Archive)
               .Single(u => u.Id == _userId)
               .Bookmarks;

           IEnumerable<Bookmark> query = allBookmarks;

           // Apply filters for tag

           bool ApplyUrlFilters(Uri urlValue, IEnumerable<IEnumerable<string>> filters)
           {
               foreach (IEnumerable<string> filter in filters)
                   if (filter.All(filterArguments => urlValue.ToString().ToLower().Contains(filterArguments.ToLower())))
                       return true;

               return false;
           }

           bool ApplyTagFilters(IEnumerable<Tag> tagValues, IEnumerable<IEnumerable<string>> filters)
           {
               string[] tagValuesLower = tagValues.Select(f => f.Name.ToLower()).ToArray();
               foreach (IEnumerable<string> filter in filters)
                   if (filter.All(filterArguments => tagValuesLower.Contains(filterArguments.ToLower())))
                       return true;

               return false;
           }

           bool ApplyFolderFilters(Folder? folderValues, IEnumerable<IEnumerable<string[]>> filters)
           {
               string[] folderNamesLower = folderValues?.ToArray().Select(folder => folder.Name.ToLower()).ToArray() ?? new string[] {};
               foreach (IEnumerable<string[]> filter in filters)
                   if (filter.All(filterArguments => folderNamesLower.SequenceEqual(filterArguments.Select(fa => fa.ToLower()))))
                       return true;

               return false;
           }

           bool ApplyContentFilter(Archive? archiveValue, IEnumerable<IEnumerable<string>> filters)
           {
               string plaintext = archiveValue?.PlainText.ToLower() ?? "";
               foreach (IEnumerable<string> filter in filters)
                   if (filter.All(filterArguments => plaintext.Contains(filterArguments.ToLower())))
                       return true;

               return false;
           }

           if (search.ArchivedFilter != null)
               query = query.Where(b => (b.Archive is not null) == search.ArchivedFilter);

           if (search.TitleFilter.Count > 0)
               query = query.Where(b => search.TitleFilter.Any(filter => b.Title.ToLower().Contains(filter.ToLower())));

           if (search.UrlFilter.Count > 0)
               query = query.Where(b => ApplyUrlFilters(b.Url, search.UrlFilter));

           if (search.TagFilter.Count > 0)
               query = query.Where(b => ApplyTagFilters(b.Tags, search.TagFilter));

           if (search.FolderFilter.Count > 0)
               query = query.Where(b => ApplyFolderFilters(b.Folder, search.FolderFilter));

           if (search.ContentFilter.Count > 0)
               query = query.Where(b => ApplyContentFilter(b.Archive, search.ContentFilter));

           // Apply sort on selected field
           if (search.SortDescending)
               query = query.OrderByDescending(orderby);
           else
               query = query.OrderBy(orderby);

           return query.ToList();
        }

        public IActionResult Index(string? search)
        {
            _userId = ulong.Parse(User.FindFirstValue(Claims.UserId));
            var searchQuery = new SearchQuery(search);
            ViewData["Search"] = searchQuery;
            ViewData["Folders"] = _context.Folders.Where(f => f.UserId == _userId).ToList();
            ViewData["Bookmarks"] = QueryBookmarks(searchQuery, 1, 500);
            
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(); //new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
        }
    }
}