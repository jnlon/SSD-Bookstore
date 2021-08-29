using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BookmarksManager;
using Bookstore.Models;

namespace Bookstore.Utilities
{
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

        public string DetectFaviconMimeType(BookmarkLink link)
        {
            // NOTE HACK: Firefox netscape export will incorrectly classify SVG favicons as 'image/png' in data URL of netscape export
            // Correct this with a heuristic; look at the canonical URL for hints
            if ((link.IconUrl?.EndsWith(".svg") ?? false) ||
                (link.IconUrl?.StartsWith("data:image/svg+xml") ?? false))
            {
                return "image/svg+xml";
            }

            return link.IconContentType;
        }

        private void ImportBookmark(string[] folderStackArray, BookmarkLink link)
        {
            // Do not import bookmarks matching existing URLs
            if (_existingBookmarks.Contains(new Uri(link.Url)))
                return;
            
            byte[]? favicon = null;
            string? faviconMime = null;
            
            if (link.IconData != null && link.IconData.Length > 0)
            {
                favicon = link.IconData;
                faviconMime = DetectFaviconMimeType(link);
            }
            
            HashSet<Tag> tags = new();

            if (link.Attributes.ContainsKey("tags"))
            {
                tags = _resolver.ResolveTags(link.Attributes["tags"].Split(','));
            }
                
            _bookstore.CreateBookmark(
                archive: null,
                created: link.Added ?? DateTime.Now,
                modified: link.Added ?? DateTime.Now,
                favicon: favicon,
                faviconMime: faviconMime,
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