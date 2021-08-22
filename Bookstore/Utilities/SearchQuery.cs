using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Bookstore.Models;

namespace Bookstore.Utilities
{
    public enum SearchQueryField { Tag, Folder, Title, Url, Archived }

    public class SearchQueryFunction
    {
        public readonly IReadOnlyList<string> Arguments;
        public List<string[]> SplitArguments;
        public SearchQueryFunction(IEnumerable<string> arguments)
        {
            Arguments = arguments.ToList();
        }

        public void SplitAndLowerArguments(Regex regex)
        {
            SplitArguments = Arguments
                .Select(arg => regex.Split(arg)
                    .Where(s => s != string.Empty)
                    .Select(s => s.ToLower())
                    .ToArray())
                .ToList();
        }

        public static List<SearchQueryFunction> FromQuery(string query, string functionName)
        {
            SearchQueryFunction ParseFunction(Match match)
            {
                string inner = match.Groups["params"].Value;
                IEnumerable<string> funcParams = inner.Split(",").Select(s => s.Trim());
                return new SearchQueryFunction(funcParams.ToList());
            }
            
            var matcher = new Regex(@$"\b{functionName}\((?<params>.*?)\)");
            return matcher.Matches(query).Select(ParseFunction).ToList();
        }
    }
    
    public class SearchQuery
    {
        private enum SortDirection { Asc, Desc }
        
        public readonly string QueryString;
        public readonly SearchQueryField SortField;
        public readonly bool SortDescending;
        public readonly bool? ArchivedFilter;

        private BookstoreService _bookstoreService;

        // Note: The inner collection performs logical-AND filter, the outer-list is logical OR
        private readonly IReadOnlyList<SearchQueryFunction> _tagFilter;
        private readonly IReadOnlyList<SearchQueryFunction> _singleFolderFilter;
        private readonly IReadOnlyList<SearchQueryFunction> _recursiveFoldersFilter; // like folder but recursive
        private readonly IReadOnlyList<SearchQueryFunction> _urlFilter;
        private readonly IReadOnlyList<SearchQueryFunction> _contentFilter;
        private readonly IReadOnlyList<SearchQueryFunction> _titleFilter;
        private readonly IReadOnlyList<SearchQueryFunction> _generalFilter;
        
        public SearchQuery(string? query, BookstoreService service)
        {
            QueryString = query ?? string.Empty;;

            _bookstoreService = service;
            
            _contentFilter = SearchQueryFunction.FromQuery(QueryString, "content");
            _singleFolderFilter = SearchQueryFunction.FromQuery(QueryString, "folder");
            _recursiveFoldersFilter = SearchQueryFunction.FromQuery(QueryString, "folders");
            _tagFilter = SearchQueryFunction.FromQuery(QueryString, "tag");
            _urlFilter = SearchQueryFunction.FromQuery(QueryString, "url");
            _titleFilter = SearchQueryFunction.FromQuery(QueryString, "title");

            // Populate SearchQueryFunction.SplitArguments using the supplied regex
            var folderSplit = new Regex(@"\s*[>/]\s*");
            
            foreach (var filter in _singleFolderFilter)
            {
                filter.SplitAndLowerArguments(folderSplit);
            }
            
            foreach (var filter in _recursiveFoldersFilter)
            {
                filter.SplitAndLowerArguments(folderSplit);
            }
            
            // Select the first argument of the first function
            ArchivedFilter = SearchQueryFunction.FromQuery(QueryString, "archived")
                .FirstOrDefault()
                ?.Arguments
                .Take(1)
                .Select(arg => bool.TryParse(arg, out bool result) ? result : (bool?) null)
                .FirstOrDefault();
            
            // Use the first sort function
            var sort = SearchQueryFunction.FromQuery(QueryString, "sort").FirstOrDefault();
            if (sort is not null && sort.Arguments.Count == 2)
            {
                if (Enum.TryParse(sort.Arguments[0], true, out SearchQueryField sortFieldParsed))
                    SortField = sortFieldParsed;
                
                if (Enum.TryParse(sort.Arguments[1], true, out SortDirection sortDirectionParsed))
                    SortDescending = sortDirectionParsed == SortDirection.Desc;
            }
            
            // all tokens that don't end with '(' and aren't inside some '()'
            var allTagRegex = new Regex(@"\s?(content|folder|folders|tag|archived|url|sort|title)\(.*?\)\s?");
            string queryWithoutFunctions = allTagRegex.Replace(QueryString, " ");
            var generalFilter = Regex.Split(queryWithoutFunctions, @"\s+")
                .Where(s => s != String.Empty)
                .ToList();

            // General filter isn't really a filter, but we put it in the same structure and treat every word as a filter argument
            if (generalFilter.Count > 0)
                _generalFilter = new List<SearchQueryFunction> {new(generalFilter)};
            else
                _generalFilter = new List<SearchQueryFunction>();
        }

        private static bool GenericPassesFilter(Func<string, bool> passesFilterArgumentPredicate, IReadOnlyList<SearchQueryFunction> filters)
        {
            if (filters.Count == 0)
                return true;
            
            // Apply AND logic inside function, apply OR logic outside function
            foreach (var filter in filters)
                if (filter.Arguments.All(passesFilterArgumentPredicate))
                    return true;
            
            return false;
        }
        
        private bool PassesTagFilter(HashSet<Tag> tags)
        {
            string[] tagStrings = tags.Select(t => t.Name).ToArray();
            bool PassesTagFilterArgument(string arg) => tagStrings.Contains(arg);
            return GenericPassesFilter(PassesTagFilterArgument, _tagFilter);
        }

        private bool PassesGeneralFilter(Bookmark bm)
        {
           bool PassesUriFilterArgument(string arg) => bm.Url.ToString().ToLower().Contains(arg.ToLower());
           bool PassesTitleFilterArgument(string arg) => bm.Title.ToLower().Contains(arg.ToLower());
           bool PassesTagFilterArgument(string arg) => bm.Tags.Any(t => t.Name.ToLower() == arg.ToLower());
           bool PassesFolderFilterArgument(string arg) => bm.Folder?.ToArray().Any(f => f.Name.ToLower() == arg.ToLower()) ?? false;

           return GenericPassesFilter(PassesUriFilterArgument, _generalFilter)
                  || GenericPassesFilter(PassesTitleFilterArgument, _generalFilter)
                  || GenericPassesFilter(PassesTagFilterArgument, _generalFilter)
                  || GenericPassesFilter(PassesFolderFilterArgument, _generalFilter);
        }
        
        private bool PassesArchiveFilter(long? archiveId)
        {
            if (ArchivedFilter == null)
                return true;

            if (archiveId is null && ArchivedFilter == false)
                return true;

            if (archiveId is not null && ArchivedFilter == true)
                return true;

            return false;
        }
        
        private bool PassesTitleFilter(string title)
        {
           bool PassesTitleFilterArgument(string arg) => title.ToLower().Contains(arg.ToLower());
           return GenericPassesFilter(PassesTitleFilterArgument, _titleFilter);
        }

        private bool PassesContentFilter(Bookmark bm)
        {
            if (_contentFilter.Count == 0)
                return true;
            
            // If content filter is present, always exclude non-archived bookmarks
            if (bm.ArchiveId == null)
                return false;

            Archive archive = _bookstoreService.GetArchiveByBookmarkId(bm.Id)!;

            // If there is no plaintext to search, it has not passed the filter
            if (archive.PlainText == null)
                return false;
            
            bool PassesContentFilterArgument(string arg) => archive.PlainText.ToLower().Contains(arg.ToLower());
            return GenericPassesFilter(PassesContentFilterArgument, _contentFilter);
        }

        private bool PassesUrlFilter(Uri url)
        {
            bool PassesUriFilterArgument(string arg) => url.ToString().ToLower().Contains(arg.ToLower());
            return GenericPassesFilter(PassesUriFilterArgument, _urlFilter);
        }

        private bool GenericFolderPasses(Folder? folder, IReadOnlyList<SearchQueryFunction> filters)
        {
            string[] folderStringArray = folder
                ?.ToArray()
                .Select(f => f.Name.ToLower())
                .ToArray() ?? new string[]{};

            foreach (var filter in filters)
                if (filter.SplitArguments.All(arg => folderStringArray.SequenceEqual(arg)))
                    return true;

            return false;
        }
        
        private bool PassesSingleFolderFilter(Folder? folder)
        {
            if (_singleFolderFilter.Count == 0)
                return true;

            return GenericFolderPasses(folder, _singleFolderFilter);
        }
        
        private bool PassesRecursiveFolderFilter(Folder? folder)
        {
            if (_recursiveFoldersFilter.Count == 0)
                return true;

            bool folderPasses = false;
            while (folder != null && folderPasses == false)
            {
                folderPasses = GenericFolderPasses(folder, _recursiveFoldersFilter);
                folder = folder.Parent;
            }
            
            return folderPasses;
        }
        
        public bool PassesAllFilters(Bookmark bm)
        {
            return
                PassesGeneralFilter(bm)
                && PassesArchiveFilter(bm.ArchiveId)
                && PassesTitleFilter(bm.Title)
                && PassesUrlFilter(bm.Url)
                && PassesTagFilter(bm.Tags)
                && PassesSingleFolderFilter(bm.Folder)
                && PassesRecursiveFolderFilter(bm.Folder)
                && PassesContentFilter(bm);
        }

        public override string ToString() => QueryString;
    }
}