using System;
using System.Linq;
using Bookstore.Models;

namespace Bookstore.Utilities
{
    public class BookstoreCsv
    {
        public string Title { get; set; }
        public Uri Url { get; set; }
        public string? Tags { get; set; }
        public string? FolderPath { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }

        public static BookstoreCsv FromBookmark(Bookmark bm)
        {
            return new BookstoreCsv
            {
                Tags = string.Join(",", bm.Tags.Select(t => t.Name).ToList()),
                Title = bm.Title.Trim(),
                Url = bm.Url,
                CreatedDate = bm.Created,
                FolderPath = bm?.Folder?.ToMenuString() ?? "",
                ModifiedDate = bm.Modified,
            };
        }
    }
}