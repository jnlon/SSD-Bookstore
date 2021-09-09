namespace Bookstore.Utilities
{
    // Standard interface for classes that export bookmarks to a specific format (CSV, Netscape, etc.) 
    public interface IBookmarkExporter
    {
        public string Export();
        public string ContentType { get; }
        public string FileName { get;  }
    }
}