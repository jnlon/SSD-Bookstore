using System.Collections.Generic;

namespace Bookstore.Models
{
    public class Tag
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public ICollection<Bookmark> Bookmarks { get; set; }
        public long UserId { get; set; }
    }
}