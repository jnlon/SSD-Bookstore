using System;
using System.Collections.Generic;
using System.Linq;
using Bookstore.Models;

namespace Bookstore.Utilities
{
    public class SearchQueryResult
    {
        public int TotalFolders { get; private set; }
        public int QueriedFolders { get; private set; }

        public int TotalTags { get; private set; }
        public int QueriedTags { get; private set; }

        public int TotalBookmarks { get; private set; }
        public int QueriedBookmarks { get; private set; }

        public SearchQuery Search { get; private set; }
        public List<Bookmark> Results { get; private set; }

        private int _page;
        private int _pageCount;

        public SearchQueryResult(SearchQuery search, int page, int pageCount)
        {
            Search = search;
            _page = page;
            _pageCount = pageCount;
        }

        public void Execute(BookstoreService bookstore)
        {
            Func<Bookmark, IComparable> orderby = Search.SortField switch
            {
                SearchQueryField.Archived => bm => bm.Archive == null,
                SearchQueryField.Folder => bm => bm.Folder?.ToMenuString() ?? string.Empty,
                SearchQueryField.Tag => bm => string.Join(",", bm.Tags.ToList().OrderBy(tag => tag.Name)),
                SearchQueryField.Title => bm => bm.Title,
                SearchQueryField.Url => bm => bm.Url.ToString()
            };

            List<Bookmark> allBookmarks = bookstore.QueryAllUserBookmarks().ToList();
            
            IEnumerable<Bookmark> query = allBookmarks.Where(Search.PassesAllFilters);

            // Apply sort on selected field
            if (Search.SortDescending)
                query = query.OrderByDescending(orderby);
            else
                query = query.OrderBy(orderby);

            Results = query.ToList();
            
            TotalBookmarks = allBookmarks.Count;
            QueriedBookmarks = Results.Count;
            
            TotalTags = bookstore.QueryAllUserTags().Count();
            QueriedTags = Results.SelectMany(bm => bm.Tags).Distinct().Count();

            TotalFolders = Results.Select(bm => bm.Folder).Distinct().Count();
            QueriedFolders = Results.Select(bm => bm.Folder).Distinct().Count();
        }
    }
}