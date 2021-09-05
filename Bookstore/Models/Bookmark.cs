using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Bookstore.Models
{
    public class Bookmark
    {
        public long Id { get; set; }
        public long? ArchiveId { get; set; }
        public Archive? Archive { get; set; }
        public long UserId { get; set; }
        public User User { get; set; }
        public long? FolderId { get; set; }
        public Folder? Folder { get; set; } // When folder is null, this bookmark exists at the root

        public string? FolderString
        {
            get { return Folder?.ToMenuString() ?? ""; }
            private set { }
        }

        public long? FaviconId { get; set; }
        public Favicon? Favicon { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        public Uri Url { get; set; }

        public string? UrlString
        {
            get { return Url.ToString().ToLower(); }
            set { }
        }

        public string Title { get; set; }
        public HashSet<Tag> Tags { get; set; }

        public string? TagString
        {
            get { return Tags is null ? "" : string.Join(" ", Tags.OrderBy(t => t.Name).Select(t => t.Name)); }
            set { }
        }

        // ????
        // public string Keywords { get; set; }
    }
}