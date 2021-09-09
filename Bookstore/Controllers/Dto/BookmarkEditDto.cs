using System;

namespace Bookstore.Controllers.Dto
{
    public class BookmarkEditDto
    {
        public Uri Url { get; set; }
        public string Title { get; set; }
        public string? Tags { get; set; }
        public long? Folder { get; set; }
        public string? CustomFolder { get; set; } // Only if user chooses to create a new folder
    }
}