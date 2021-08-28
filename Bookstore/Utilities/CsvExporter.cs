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