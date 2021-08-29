using System.IO;

namespace Bookstore.Utilities
{
    public interface IBookmarkImporter
    {
        public int Import(Stream csvStream);
    }
}