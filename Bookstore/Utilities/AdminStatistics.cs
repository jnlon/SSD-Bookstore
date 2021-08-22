namespace Bookstore.Utilities
{
    public class AdminStatistics
    {
        public int NumberOfUsers { get; set; }
        public int TotalNumberOfBookmarks { get; set; }
        public int TotalNumberOfArchivedBookmarks { get; set; }
        public long ArchivedDiskUsageBytes { get; set; }
        public long TotalSpaceOnDisk { get; set; }
    }
}