using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bookstore.Models
{
    public class Archive
    {
        [ForeignKey("Bookmark")]
        public ulong Id { get; set; }
        public string PlainText { get; set; }
        public DateTime Created { get; set; }
    }
}