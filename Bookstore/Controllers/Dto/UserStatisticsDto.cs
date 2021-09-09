using Bookstore.Models;

namespace Bookstore.Controllers.Dto
{
    // DTO containing bookmark statistics for user, used on admin homepage
    public class UserStatisticsDto
    {
        public User User { get; set; }
        public int NumberOfBookmarks { get; set; }
        public int NumberOfArchivedBookmarks { get; set; }
        public long ArchivedBookmarkDiskUsage { get; set; }
    }
}