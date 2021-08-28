using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Bookstore.Models;
using CsvHelper;

namespace Bookstore.Utilities
{
    public class CsvImporter
    {
        private BookstoreService _service;
        private BookstoreResolver _resolver;
        
        public CsvImporter(BookstoreService service)
        {
            _service = service;
            _resolver = new BookstoreResolver(service);
        }
        
        private Bookmark ImportBookmark(BookstoreCsv record)
        {
            Folder? folder = null;
            HashSet<Tag> tags = new();
            byte[]? favicon = null;
            string? faviconMime = null;

            if (!string.IsNullOrEmpty(record.FaviconBase64))
            {
                favicon = Convert.FromBase64String(record.FaviconBase64);
                faviconMime = record.FaviconMime;
            }

            if (!string.IsNullOrEmpty(record.FolderPath))
                folder = _resolver.ResolveFolder(record.FolderPath.Split(Folder.Seperator));
            
            if (!string.IsNullOrEmpty(record.Tags))
                tags = _resolver.ResolveTags(record.Tags.Split(","));
            
            return _service.CreateBookmark(
                archive: null,
                created: record.CreatedDate,
                modified: record.ModifiedDate,
                favicon: favicon,
                faviconMime: faviconMime,
                folder: folder,
                tags: tags,
                title: record.Title,
                url: record.Url
            );
        }
        
        public void Import(Stream csvStream)
        {
            using var reader = new StreamReader(csvStream);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            foreach (var record in csv.GetRecords<BookstoreCsv>())
            {
                ImportBookmark(record);
            }
        }
    }
}