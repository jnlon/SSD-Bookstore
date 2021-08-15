using System;
using System.Collections;
using System.Collections.Generic;

namespace Bookstore.Models
{
    public class Bookmark
    {
        public ulong Id { get; set; }
        public Archive? Archive { get; set; }
        public ulong UserId { get; set; }
        public User User { get; set; }
        public Folder? Folder { get; set; } // When folder is null, this bookmark exists at the root
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        public Uri Url { get; set; }
        public string Title { get; set; }
        public byte[]? Favicon { get; set; }
        public string? FaviconMime { get; set; }
        public HashSet<Tag> Tags { get; set; }
        
        // ????
        // public string Keywords { get; set; }
    }
}