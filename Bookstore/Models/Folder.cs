using System.Collections.Generic;
using System.Linq;

namespace Bookstore.Models
{
    // Class representing a bookmark folder. This data structure is recursive, ie. self-referencing via the 'Parent' property
    public class Folder
    {
        public static string Seperator = "/";
        
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
            return string.Join($"{Seperator}", ToArray().Select(f => f.Name));
        }
    }
}