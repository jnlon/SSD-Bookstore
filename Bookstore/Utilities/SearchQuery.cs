using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Bookstore.Utilities
{
    public enum SearchQueryField { Tag, Folder, Title, Url, Archived }
    
    public class SearchQuery
    {
        private enum SortDirection { Asc, Desc }
        
        public readonly string Query;
        public readonly SearchQueryField SortField;
        public readonly bool SortDescending;
        public readonly bool? ArchivedFilter;

        // Note: The inner collection performs logical-AND filter, the outer-list is logical OR
        public readonly IReadOnlyList<IReadOnlyList<string>> TagFilter;
        public readonly IReadOnlyList<IReadOnlyList<string[]>> FolderFilter;
        public readonly IReadOnlyList<IReadOnlyList<string>> UrlFilter;
        public readonly IReadOnlyList<IReadOnlyList<string>> ContentFilter;
        public readonly IReadOnlyList<string> TitleFilter;

        private static List<List<string>> ParseMultiStrings(MatchCollection matches)
        {
            List<string> Tokenize(Match match)
            {
                string inner = match.Groups["params"].Value;
                IEnumerable<string> funcParams = inner.Split(",").Select(s => s.Trim());
                return funcParams.ToList();
            }
            
            return matches.Select(Tokenize).ToList();
        }

        private static bool? ParseBool(Match? match, bool? defaultValue)
        {
            if (match == null)
                return defaultValue;
            
            if (bool.TryParse(match.Groups["params"].Value, out bool result))
                return result;

            return defaultValue;
        }

        private static T ParseEnum<T>(Match? match, T defaultValue) where T : struct
        {
            if (match == null)
                return defaultValue;

            if (Enum.TryParse(match.Groups["params"].Value, true, out T value))
                return value;
            
            return defaultValue;
        }
        
        public SearchQuery(string? query)
        {
            Query = query ?? string.Empty;;
            
            var content = new Regex(@"\bcontent\((?<params>.*?)\)");
            var folder = new Regex(@"\bfolder\((?<params>.*?)\)");
            var tag = new Regex(@"\btag\((?<params>.*?)\)");
            var url = new Regex(@"\burl\((?<params>.*?)\)");
            var archived = new Regex(@"\barchived\((?<params>(true|false))\)");
            var sortField = new Regex(@"\bsort\((?<params>(tag|folder|title|url|archived)), .*?\)");
            var sortDirection = new Regex(@"\bsort\(.*?, (?<params>(asc|desc))\)");

            var folderSplit = new Regex(@"\s+?>\s+?");

            ContentFilter = ParseMultiStrings(content.Matches(Query));
            TagFilter = ParseMultiStrings(tag.Matches(Query));
            UrlFilter = ParseMultiStrings(url.Matches(Query));
            ArchivedFilter = ParseBool(archived.Match(Query), null);
            SortField = ParseEnum(sortField.Match(Query), SearchQueryField.Title);
            SortDescending = ParseEnum(sortDirection.Match(Query), SortDirection.Asc) == SortDirection.Desc;
            FolderFilter = ParseMultiStrings(folder.Matches(Query))
                .Select(inner => 
                        inner.Select(arg => 
                            folderSplit.Split(arg)).ToList()).ToList();
            
            // all tokens that don't end with '(' and aren't inside some '()'
            var allTagRegex = new Regex(@"\s?(content|folder|tag|archived|url|sort)\(.*?\)\s?");
            string queryWithoutFunctions = allTagRegex.Replace(Query, " ");
            TitleFilter = Regex.Split(queryWithoutFunctions, @"\s+")
                .Where(s => s != String.Empty)
                .ToList();
        }

        public override string ToString() => Query;
    }
}