using System.Collections.Generic;

namespace Bookstore.Models
{
    public class User
    {
        public long Id { get; set; }
        public string Username { get; set; }
        public bool Admin { get; set; }
        public Settings Settings { get; set; }
        public List<Bookmark> Bookmarks { get; set; }
        public string PasswordHash { get; set; }
        public string PasswordSalt { get; set; }
    }
}