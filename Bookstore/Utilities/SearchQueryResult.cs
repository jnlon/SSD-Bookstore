using System;
using System.Collections.Generic;
using System.Linq;
using Bookstore.Models;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Utilities
{
    public class SearchQueryResult
    {
        public int TotalFolders { get; private set; }
        public int QueriedFolders { get; private set; }

        public int TotalTags { get; private set; }
        public int QueriedTags { get; private set; }

        public int TotalBookmarks { get; private set; }
        public int TotalQueriedBookmarks { get; private set; }
        public int QueriedBookmarks { get; private set; }

        public SearchQuery Search { get; private set; }
        public List<Bookmark> Results { get; private set; }

        private int _page;
        private int _itemsPerPage;

        public SearchQueryResult(SearchQuery search, int page, int itemsPerPage)
        {
            Search = search;
            _page = Math.Max(1, page);
            _itemsPerPage = itemsPerPage;
        }

        public void Execute(BookstoreService bookstore)
        {
            // Bookmark[] allBookmarks = bookstore.QueryAllUserBookmarks().ToArray();
            // IEnumerable<Bookmark> query = allBookmarks.Where(Search.PassesAllFilters);

            IQueryable<Bookmark> query = bookstore.QueryAllUserBookmarks();
            
            // Passes General Filter
            foreach (var filter in Search.GeneralFilter)
            {
                query = query.Where(bm => 
                    EF.Functions.Like(bm.UrlString, "%" + filter.Argument + "%") || // URL Matches
                    EF.Functions.Like(bm.Title, "%" + filter.Argument + "%") ||     // Title Matches
                    EF.Functions.Like(bm.TagString, "%" + filter.Argument + "%") || // Tag matches
                    EF.Functions.Like(bm.FolderString, "%" + filter.Argument + "%") // Folder matches
                   //  (bm.Archive != null && bm.Archive.PlainText != null && EF.Functions.Like(bm.Archive.PlainText, "%" + argument + "%")) // Archive Text matches
                );
            }
            
            // archived()
            if (Search.ArchivedFilter != null)
                query = query.Where(bm => (bm.ArchiveId == null) == (Search.ArchivedFilter == false));
            
            // title()
            foreach (var filter in Search.TitleFilter)
                query = query.Where(bm => EF.Functions.Like(bm.Title, "%" + filter.Argument + "%"));
            
            // url()
            foreach (var filter in Search.UrlFilter)
                query = query.Where(bm => EF.Functions.Like(bm.UrlString, "%" + filter.Argument + "%"));

            // folders()
            foreach (var filter in Search.RecursiveFoldersFilter)
                query = query.Where(bm => EF.Functions.Like(bm.FolderString, "%" + filter.Argument + "%"));
            
            // folder()
            foreach (var filter in Search.SingleFolderFilter)
                query = query.Where(bm => EF.Functions.Like(bm.FolderString, filter.Argument));
            
            // tag()
            foreach (var filter in Search.TagFilter)
                query = query.Where(bm => EF.Functions.Like(bm.TagString, "%" + filter.Argument + "%"));
            
            // intext()
            foreach (var filter in Search.IntextFilter)
            {
                query = query.Include(bm => bm.Archive);
                query = query.Where(bm =>
                    bm.Archive != null &&
                    bm.Archive.PlainText != null &&
                    EF.Functions.Like(bm.Archive.PlainText, "%" + filter.Argument + "%"));
            }
            
            // Apply sort on selected field
            if (Search.SortField is null)
            {
                query = query.OrderBy(bm => bm.Id);
            }
            else
            {
                if (Search.SortDescending)
                {
                    if (Search.SortField == SearchQueryField.Archived) query = query.OrderByDescending(bm => bm.ArchiveId == null);
                    else if (Search.SortField == SearchQueryField.Folder) query = query.OrderByDescending(bm => bm.FolderString);
                    else if (Search.SortField == SearchQueryField.Tag) query = query.OrderByDescending(bm => bm.TagString);
                    else if (Search.SortField == SearchQueryField.Title) query = query.OrderByDescending(bm => bm.Title);
                    else if (Search.SortField == SearchQueryField.Url) query = query.OrderByDescending(bm => bm.UrlString);
                    else if (Search.SortField == SearchQueryField.Modified) query = query.OrderByDescending(bm => bm.Modified);
                    else if (Search.SortField == SearchQueryField.Domain) query = query.OrderByDescending(bm => bm.DomainString);
                }
                else
                {
                    if (Search.SortField == SearchQueryField.Archived) query = query.OrderBy(bm => bm.ArchiveId == null);
                    else if (Search.SortField == SearchQueryField.Folder) query = query.OrderBy(bm => bm.FolderString);
                    else if (Search.SortField == SearchQueryField.Tag) query = query.OrderBy(bm => bm.TagString);
                    else if (Search.SortField == SearchQueryField.Title) query = query.OrderBy(bm => bm.Title);
                    else if (Search.SortField == SearchQueryField.Url) query = query.OrderBy(bm => bm.UrlString);
                    else if (Search.SortField == SearchQueryField.Modified) query = query.OrderBy(bm => bm.Modified);
                    else if (Search.SortField == SearchQueryField.Domain) query = query.OrderBy(bm => bm.DomainString);
                }
            }

            TotalQueriedBookmarks = query.Count();

            // Apply Pagination
            Results = query
                .Skip(_itemsPerPage * (_page - 1))
                .Take(_itemsPerPage)
                .ToList();
            
            TotalBookmarks = bookstore.QueryAllUserBookmarks().Count();
            QueriedBookmarks = Results.Count;
            
            TotalTags = bookstore.QueryAllUserTags().Count();
            QueriedTags = Results.SelectMany(bm => bm.Tags).Distinct().Count();

            TotalFolders = Results.Select(bm => bm.Folder).Distinct().Count();
            QueriedFolders = Results.Select(bm => bm.Folder).Distinct().Count();
        }
    }
}