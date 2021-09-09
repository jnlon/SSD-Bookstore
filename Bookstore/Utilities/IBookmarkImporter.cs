using System.IO;

namespace Bookstore.Utilities
{
    // Standard interface for classes that import bookmarks from a specific format (CSV, Netscape, etc.) 
    public interface IBookmarkImporter
    {
        public int Import(Stream csvStream);
    }
}