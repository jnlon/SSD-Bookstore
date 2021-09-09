using System;
using System.Collections.Generic;
using System.Linq;
using BookmarksManager;
using Bookstore.Models;

namespace Bookstore.Utilities
{
    // Helper class to export bookmarks to Netscape Bookmarks HTML format
    public class NetscapeExporter : IBookmarkExporter
    {
        private BookstoreService _service;

        public string ContentType => "text/html";
        public string FileName => $"bookmarks_{DateTime.Now:yyyy-MM-d}.html";
        public NetscapeExporter(BookstoreService service)
        {
            _service = service;
        }

        private static BookmarkLink ExportBookmark(Bookmark bookmark)
        {
            var attributes = new Dictionary<string, string>();

            if (bookmark.Tags.Count > 0)
                attributes["tags"] = string.Join(",", bookmark.Tags.Select(t => t.Name));
            
            return new BookmarkLink
            {
                Url = bookmark.Url.ToString(),
                Added = bookmark.Created,
                Title = bookmark.Title,
                LastModified = bookmark.Modified,
                Attributes = attributes
            };
        }

        // Recursive method
        private void ExportFolders(Folder folder, BookmarkFolder parent)
        {
            var childBookmarks = _service.QueryAllUserBookmarks()
                .Where(bm => (bm.FolderId) == folder.Id)
                .ToList();

            var exportFolder = new BookmarkFolder(folder.Name);
            parent.Add(exportFolder);
            
            // Add all child bookmarks
            foreach (var bookmark in childBookmarks)
            {
                exportFolder.Add(ExportBookmark(bookmark));
            }

            var childFolders = _service.QueryAllUserFolders()
                .Where(f => f.ParentId == folder.Id)
                .ToList();
            
            // Add all the sub-folders
            foreach (var childFolder in childFolders)
            {
                ExportFolders(childFolder, exportFolder);
            }
        }

        public string Export()
        {
            var root = new BookmarkFolder();
            
            // Query all bookmarks without a root folder, and put it in netscape export root
            foreach (Bookmark bookmark in _service.QueryAllUserBookmarks().Where(bm => bm.FolderId == null))
            {
                root.Add(ExportBookmark(bookmark));
            }
            
            // Query all root folders
            // For each root folder
            // * Add all bookmarks in that folder
            // For each child folder
            // Create the folder in current folder
            // Set child folder as new current folder
            // Recurse to *
            
            var rootFolders = _service.QueryAllUserFolders().Where(f => f.ParentId == null).ToList();
            foreach (var folder in rootFolders)
            {
                ExportFolders(folder, root);
            }

            var writer = new NetscapeBookmarksWriter(root);
            return writer.ToString();
        }
    }
}