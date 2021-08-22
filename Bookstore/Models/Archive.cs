using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bookstore.Models
{
    public class Archive
    {
        [ForeignKey("Bookmark")]
        public long Id { get; set; }
        public long UserId { get; set; }
        public string? PlainText { get; set; }
        public byte[]? Formatted { get; set; }
        public byte[] Bytes { get; set; }
        public string Mime { get; set; }
        public DateTime Created { get; set; }
    }
}