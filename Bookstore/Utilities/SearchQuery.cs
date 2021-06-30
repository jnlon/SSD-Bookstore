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

        // Note: The inner collection performs logical-AND filter, the outer-list is logical OR
        private readonly IReadOnlyList<SearchQueryFunction> TagFilter;
        private readonly IReadOnlyList<SearchQueryFunction> FolderFilter;
        private readonly IReadOnlyList<SearchQueryFunction> UrlFilter;
        private readonly IReadOnlyList<SearchQueryFunction> ContentFilter;
        private readonly IReadOnlyList<SearchQueryFunction> TitleFilter;
        private readonly IReadOnlyList<SearchQueryFunction> GeneralFilter;
        
        public SearchQuery(string? query)
        {
            QueryString = query ?? string.Empty;;
            
            ContentFilter = SearchQueryFunction.FromQuery(QueryString, "content");
            FolderFilter = SearchQueryFunction.FromQuery(QueryString, "folder");
            TagFilter = SearchQueryFunction.FromQuery(QueryString, "tag");
            UrlFilter = SearchQueryFunction.FromQuery(QueryString, "url");
            TitleFilter = SearchQueryFunction.FromQuery(QueryString, "title");

            // Populate SearchQueryFunction.SplitArguments using the supplied regex
            var folderSplit = new Regex(@"[>/]");
            foreach (var filter in FolderFilter)
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
            var sort = SearchQueryFunction.FromQuery(QueryString, "title").FirstOrDefault();
            if (sort is not null && sort.Arguments.Count == 2)
            {
                if (Enum.TryParse(sort.Arguments[0], out SearchQueryField sortFieldParsed))
                    SortField = sortFieldParsed;
                
                if (Enum.TryParse(sort.Arguments[1], out SortDirection sortDirectionParsed))
                    SortDescending = sortDirectionParsed == SortDirection.Desc;
            }
            
            // all tokens that don't end with '(' and aren't inside some '()'
            var allTagRegex = new Regex(@"\s?(content|folder|tag|archived|url|sort|title)\(.*?\)\s?");
            string queryWithoutFunctions = allTagRegex.Replace(QueryString, " ");
            var generalFilter = Regex.Split(queryWithoutFunctions, @"\s+")
                .Where(s => s != String.Empty)
                .ToList();

            // General filter isn't really a filter, but we put it in the same structure and treat every word as a filter argument
            if (generalFilter.Count > 0)
                GeneralFilter = new List<SearchQueryFunction> {new(generalFilter)};
            else
                GeneralFilter = new List<SearchQueryFunction>();
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
            return GenericPassesFilter(PassesTagFilterArgument, TagFilter);
        }

        private bool PassesGeneralFilter(Bookmark bm)
        {
           bool PassesUriFilterArgument(string arg) => bm.Url.ToString().ToLower().Contains(arg.ToLower());
           bool PassesTitleFilterArgument(string arg) => bm.Title.ToLower().Contains(arg.ToLower());
           
           return GenericPassesFilter(PassesUriFilterArgument, GeneralFilter)
                  && GenericPassesFilter(PassesTitleFilterArgument, GeneralFilter);
        }
        
        private bool PassesArchiveFilter(Archive? archive)
        {
            if (ArchivedFilter == null)
                return true;

            if (archive is null && ArchivedFilter == false)
                return true;

            if (archive is not null && ArchivedFilter == true)
                return true;

            return false;
        }
        
        private bool PassesTitleFilter(string title)
        {
           bool PassesTitleFilterArgument(string arg) => title.ToLower().Contains(arg.ToLower());
           return GenericPassesFilter(PassesTitleFilterArgument, TitleFilter);
        }

        private bool PassesContentFilter(Archive? archive)
        {
            // If content filter is present, always exclude non-archived bookmarks
            if (ContentFilter.Count > 0 && archive == null)
                return false;
            
            bool PassesContentFilterArgument(string arg) => archive.PlainText.ToLower().Contains(arg.ToLower());
            return GenericPassesFilter(PassesContentFilterArgument, ContentFilter);
        }

        private bool PassesUrlFilter(Uri url)
        {
            bool PassesUriFilterArgument(string arg) => url.ToString().ToLower().Contains(arg.ToLower());
            return GenericPassesFilter(PassesUriFilterArgument, UrlFilter);
        }
        
        private bool PassesFolderFilter(Folder? folder)
        {
            if (FolderFilter.Count == 0)
                return true;
            
            string[] folderStringArray = folder
                ?.ToArray()
                .Select(f => f.Name.ToLower())
                .ToArray() ?? new string[]{};

            foreach (var filter in FolderFilter)
                if (filter.SplitArguments.All(arg => folderStringArray.SequenceEqual(arg)))
                    return true;
            
            return false;
        }

        public bool PassesAllFilters(Bookmark bm)
        {
            return
                PassesGeneralFilter(bm)
                && PassesArchiveFilter(bm.Archive)
                && PassesTitleFilter(bm.Title)
                && PassesUrlFilter(bm.Url)
                && PassesTagFilter(bm.Tags)
                && PassesFolderFilter(bm.Folder)
                && PassesContentFilter(bm.Archive);
        }

        public override string ToString() => QueryString;
    }
}