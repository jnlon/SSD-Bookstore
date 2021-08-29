namespace Bookstore.Utilities
{
    public interface IBookmarkExporter
    {
        public string Export();
        public string ContentType { get; }
        public string FileName { get;  }
    }
}