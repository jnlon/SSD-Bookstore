using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Bookstore.Models;
using CsvHelper;

namespace Bookstore.Utilities
{

    public class BookstoreCsv
    {
        public string Title { get; private set; }
        public Uri Url  { get; private set; }
        public HashSet<Tag> Tags { get; private set; }
        public string FolderPath { get; private set; }
        public DateTime CreatedDate { get; private set; }
        public DateTime ModifiedDate { get; private set; }

        public static BookstoreCsv FromBookmark(Bookmark bm)
        {
            return new BookstoreCsv
            {
                Tags = bm.Tags,
                Title = bm.Title,
                Url = bm.Url,
                CreatedDate = bm.Created,
                FolderPath = bm?.Folder?.ToMenuString() ?? "",
                ModifiedDate = bm.Modified,
            };
        }

        public Bookmark ToBookmark()
        {
            return new Bookmark()
            {
                Title = Title,
                Url = Url,
                Tags = Tags,
                
                // TODO: We need a way of looking up appropriate folder, or making it...
                // How about folder.ParseFromMenuString() ? And the menu separator can be set as a class constant...
                Folder = null, 
                
                Created = CreatedDate,
                Modified = ModifiedDate,
            };
        }
    }
    
    
    public class CsvExporter
    {
        private BookstoreService _service;
        
        public CsvExporter(BookstoreService service)
        {
            _service = service;
        }

        public string Export()
        {
            var bookmarks = _service.QueryAllUserBookmarks().AsEnumerable().Select(BookstoreCsv.FromBookmark);
            using var writer = new StringWriter();
            using var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture);
            
            csvWriter.WriteRecords(bookmarks);
            csvWriter.Flush();

            return writer.ToString()!;
        }
    }
}