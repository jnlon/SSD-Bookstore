using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Bookstore.Models;
using CsvHelper;

namespace Bookstore.Utilities
{
    // Helper class to import bookmarks from CSV format
    public class CsvImporter : IBookmarkImporter
    {
        private BookstoreService _service;
        private BookstoreResolver _resolver;
        private int _importCount;
        private List<Uri> _existingBookmarks;
        
        public CsvImporter(BookstoreService service)
        {
            _service = service;
            _resolver = new BookstoreResolver(service);
            _importCount = 0;
            _existingBookmarks = _service.QueryAllUserBookmarks().Select(bm => bm.Url).ToList();
        }
        
        private Bookmark? ImportBookmark(BookstoreCsv record)
        {
            // Do not import bookmarks matching existing URLs
            if (_existingBookmarks.Contains(record.Url))
                return null;
            
            Folder? folder = null;
            HashSet<Tag> tags = new();

            if (!string.IsNullOrEmpty(record.FolderPath))
                folder = _resolver.ResolveFolder(record.FolderPath.Split(Folder.Seperator));
            
            if (!string.IsNullOrEmpty(record.Tags))
                tags = _resolver.ResolveTags(record.Tags.Split(","));
            
            var bookmark = _service.CreateBookmark(
                archive: null,
                created: record.CreatedDate,
                modified: record.ModifiedDate,
                faviconData: null,
                faviconMime: null,
                faviconUrl: null,
                folder: folder,
                tags: tags,
                title: record.Title,
                url: record.Url
            );
            
            _importCount += 1;

            return bookmark;
        }
        
        public int Import(Stream csvStream)
        {
            using var reader = new StreamReader(csvStream);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            foreach (var record in csv.GetRecords<BookstoreCsv>())
            {
                ImportBookmark(record);
            }
            return _importCount;
        }
    }
}