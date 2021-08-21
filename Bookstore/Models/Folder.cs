using System.Collections.Generic;
using System.Linq;

namespace Bookstore.Models
{
    public class Folder
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string Name { get; set; }
        public Folder? Parent { get; set; } // When parent is null, this folder exists at the root
        public long? ParentId { get; set; }
        public List<Bookmark> Bookmarks { get; set; }

        private static Folder[] ToArray(Folder folder)
        {
            var folders = new Stack<Folder>(new [] {folder});
            while ((folder = folder?.Parent) != null)
                folders.Push(folder);
            return folders.ToArray();
        }
        
        public Folder[] ToArray()
        {
            return ToArray(this);
        }

        public string[] ToStringArray()
        {
            return ToArray().Select(f => f.Name).ToArray();
        }

        public string ToMenuString()
        {
            return string.Join(" > ", ToArray().Select(f => f.Name));
        }
    }
}