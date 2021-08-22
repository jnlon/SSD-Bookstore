using Bookstore.Models;

namespace Bookstore.Utilities
{
    public class UserStatistics
    {
        public User User { get; set; }
        public int NumberOfBookmarks { get; set; }
        public int NumberOfArchivedBookmarks { get; set; }
        public long ArchivedBookmarkDiskUsage { get; set; }
    }
}