using System.Collections.Generic;
using System.Linq;
using Bookstore.Models;

namespace Bookstore.Utilities
{
    // Resolver methods will create new bookmark/tag if it doesnt exist
    public class BookstoreResolver
    {
        private BookstoreService _bookstore;
        private List<Folder> _folders;
        private List<Tag> _tags;

        public BookstoreResolver(BookstoreService bookstore)
        {
            _bookstore = bookstore;
            _folders = bookstore.QueryAllUserFolders().ToList();
            _tags = bookstore.QueryAllUserTags().ToList();
        }

        public Folder ResolveFolder(string[] folderPath)
        {
            return FindExistingFolder(folderPath) ?? CreateFolderFromPath(folderPath);
        }
        
        private Folder? FindExistingFolder(string[] folderPath)
        {
            return _folders.FirstOrDefault(f => f.ToStringArray().SequenceEqual(folderPath));
        }
        
        private Folder CreateFolderFromPath(string[] folderPath)
        {
            int resolvedDepth = 0;
            Folder? resolved = null;
            for (int i=0; i <= folderPath.Length; i++)
            {
                resolved = FindExistingFolder(folderPath.SkipLast(i).ToArray());
                if (resolved != null)
                {
                    resolvedDepth = folderPath.Length - i;
                    break;
                }
            }

            foreach (var unresolvedFolderSegment in folderPath.Skip(resolvedDepth).ToArray())
            {
                Folder newFolder = _bookstore.CreateFolder(unresolvedFolderSegment, resolved);
                _folders.Add(newFolder);
                resolved = newFolder;
            }

            return resolved!;
        }
        
        public HashSet<Tag> ResolveTags(IEnumerable<string> tagNames)
        {
           return tagNames.Select(ResolveTag).ToHashSet();
        }
        
        private Tag ResolveTag(string tagName)
        {
           // Create new tag if it does not exist
           Tag? tag = _tags.FirstOrDefault(t => t.Name == tagName);
           
           if (tag is null)
           {
               tag = _bookstore.CreateNewTag(tagName);
               _tags.Add(tag);
           }
           
           return tag;
        }
    }
}