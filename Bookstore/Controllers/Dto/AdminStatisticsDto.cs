namespace Bookstore.Controllers.Dto
{
    // DTO containing statistics shown on admin homepage
    public class AdminStatisticsDto
    {
        public int NumberOfUsers { get; set; }
        public int TotalNumberOfBookmarks { get; set; }
        public int TotalNumberOfArchivedBookmarks { get; set; }
        public long ArchivedDiskUsageBytes { get; set; }
        public long TotalSpaceOnDisk { get; set; }
    }
}