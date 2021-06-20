using System.Collections.Generic;
using System.Linq;

namespace Bookstore.Models
{
    public class Folder
    {
        public ulong Id { get; set; }
        public ulong UserId { get; set; }
        public string Name { get; set; }
        public Folder? Parent { get; set; } // When parent is null, this folder is exists at the root
        public ulong? ParentId { get; set; }
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

        public string ToMenuString()
        {
            return string.Join(" > ", ToArray().Select(f => f.Name));
        }
    }
}