using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BookmarksManager;
using Bookstore.Models;

namespace Bookstore.Utilities
{
    // Helper class to import bookmarks from Netscape Bookmarks HTML format
    public class NetscapeImporter : IBookmarkImporter
    {
        private BookstoreResolver _resolver;
        private BookstoreService _bookstore;
        private int _importCount;
        private List<Uri> _existingBookmarks;

        public NetscapeImporter(BookstoreService bookstore)
        {
            _bookstore = bookstore;
            _resolver = new BookstoreResolver(bookstore);
            _importCount = 0;
            _existingBookmarks = _bookstore.QueryAllUserBookmarks().Select(bm => bm.Url).ToList();
        }

        private void ImportBookmark(string[] folderStackArray, BookmarkLink link)
        {
            // Do not import bookmarks matching existing URLs
            if (_existingBookmarks.Contains(new Uri(link.Url)))
                return;
            
            HashSet<Tag> tags = new();

            if (link.Attributes.ContainsKey("tags"))
            {
                tags = _resolver.ResolveTags(link.Attributes["tags"].Split(','));
            }
                
            _bookstore.CreateBookmark(
                archive: null,
                created: link.Added ?? DateTime.Now,
                modified: link.Added ?? DateTime.Now,
                faviconData: null,
                faviconMime: null,
                faviconUrl: null,
                folder: _resolver.ResolveFolder(folderStackArray),
                tags: tags,
                title: link.Title,
                url: new Uri(link.Url)
            );
            
            _importCount += 1;
        }

        private void ImportFolder(Stack<string> folderStack, BookmarkFolder currentFolder)
        {
            string[] folderStackArray = folderStack.Reverse().ToArray(); // Note: Avoid allocating new array each loop iteration
            foreach (var link in currentFolder.Where(entry => entry is BookmarkLink))
                ImportBookmark(folderStackArray, (BookmarkLink)link);

            foreach (var nextFolder in currentFolder.Where(entry => entry is BookmarkFolder))
            {
                folderStack.Push(nextFolder.Title);
                ImportFolder(folderStack, (BookmarkFolder)nextFolder);
                folderStack.Pop();
            }
        }

        public int Import(Stream stream)
        {
           var parsed = new NetscapeBookmarksReader().Read(stream);
           ImportFolder(new Stack<string>(), parsed);
           return _importCount;
        }
    }
}