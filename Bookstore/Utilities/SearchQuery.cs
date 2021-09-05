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
        public readonly string Argument;
        public SearchQueryFunction(string argument)
        {
            Argument = argument;
        }

        public static List<SearchQueryFunction> FromQuery(string query, string functionName)
        {
            SearchQueryFunction ParseFunction(Match match)
            {
                string inner = match.Groups["params"].Value;
                return new SearchQueryFunction(inner);
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
        public readonly IReadOnlyList<SearchQueryFunction> TagFilter;
        public readonly IReadOnlyList<SearchQueryFunction> SingleFolderFilter;
        public readonly IReadOnlyList<SearchQueryFunction> RecursiveFoldersFilter; // like folder but recursive
        public readonly IReadOnlyList<SearchQueryFunction> UrlFilter;
        public readonly IReadOnlyList<SearchQueryFunction> IntextFilter;
        public readonly IReadOnlyList<SearchQueryFunction> TitleFilter;
        public readonly IReadOnlyList<SearchQueryFunction> GeneralFilter;
        
        public SearchQuery(string? query, BookstoreService service)
        {
            QueryString = query ?? string.Empty;;

            _bookstoreService = service;
            
            IntextFilter = SearchQueryFunction.FromQuery(QueryString, "intext");
            SingleFolderFilter = SearchQueryFunction.FromQuery(QueryString, "folder");
            RecursiveFoldersFilter = SearchQueryFunction.FromQuery(QueryString, "folders");
            TagFilter = SearchQueryFunction.FromQuery(QueryString, "tag");
            UrlFilter = SearchQueryFunction.FromQuery(QueryString, "url");
            TitleFilter = SearchQueryFunction.FromQuery(QueryString, "title");

            // Select the first argument of the first function
            ArchivedFilter =
                bool.TryParse(SearchQueryFunction.FromQuery(QueryString, "archived").FirstOrDefault()?.Argument, out bool result)
                    ? result
                    : null;
            
            // Use the first sort function
            var sort = SearchQueryFunction.FromQuery(QueryString, "sort").FirstOrDefault();
            if (sort is not null)
            {
                string[] sortArgs = sort.Argument.Split(",").Select(a => a.Trim()).ToArray();
                
                if (Enum.TryParse(sortArgs[0], true, out SearchQueryField sortFieldParsed))
                    SortField = sortFieldParsed;
                
                if (Enum.TryParse(sortArgs[1], true, out SortDirection sortDirectionParsed))
                    SortDescending = sortDirectionParsed == SortDirection.Desc;
            }
            
            // all tokens that don't end with '(' and aren't inside some '()'
            var allTagRegex = new Regex(@"\s?(intext|folder|folders|tag|archived|url|sort|title)\(.*?\)\s?");
            string queryWithoutFunctions = allTagRegex.Replace(QueryString, " ");
            var generalFilter = Regex.Split(queryWithoutFunctions, @"\s+")
                .Where(s => s != String.Empty)
                .Select(f => new SearchQueryFunction(f))
                .ToList();

            if (generalFilter.Count > 0)
                GeneralFilter = generalFilter;
            else
                GeneralFilter = new List<SearchQueryFunction>();
        }
        
        public override string ToString() => QueryString;
    }
}