using System;
using System.Buffers.Text;
using System.Collections.Generic;
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
        public string? FaviconBase64 { get; set; }
        public string? FaviconMime { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }

        public static BookstoreCsv FromBookmark(Bookmark bm)
        {
            return new BookstoreCsv
            {
                Tags = string.Join(",", bm.Tags.Select(t => t.Name).ToList()),
                Title = bm.Title,
                Url = bm.Url,
                CreatedDate = bm.Created,
                FolderPath = bm?.Folder?.ToMenuString() ?? "",
                ModifiedDate = bm.Modified,
                FaviconBase64 = Convert.ToBase64String(bm.Favicon ?? new byte[]{}),
                FaviconMime = bm.FaviconMime ?? ""
            };
        }
    }
}