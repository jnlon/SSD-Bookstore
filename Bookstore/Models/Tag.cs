using System.Collections;
using System.Collections.Generic;

namespace Bookstore.Models
{
    public class Tag
    {
        public ulong Id { get; set; }
        public string Name { get; set; }
        public ICollection<Bookmark> Bookmarks { get; set; }
        public ulong UserId { get; set; }
    }
}